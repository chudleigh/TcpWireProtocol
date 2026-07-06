using System;
using System.Buffers.Binary;
using System.Security.Cryptography;

namespace TcpWireProtocol.Security;

/// <summary>
/// AES-GCM (AEAD) with a deterministic per-direction counter nonce.
/// The nonce is never transmitted: both sides derive it as
/// <c>[direction(4, LE) | counter(8, LE)]</c>, where the counter starts at 0 on every
/// connection and increments per frame. TCP guarantees ordered, lossless delivery, so the
/// sender's counter and the receiver's expected counter stay in lockstep with no bytes on
/// the wire. A replayed, reordered or dropped frame decrypts against the "wrong" nonce and
/// fails the tag check (<see cref="WireDecodeStatus.Corrupt"/>) — replay protection for free.
/// The length is not encrypted but is authenticated: the 4 length bytes are fed into the
/// body's encryption as associated data (AAD).
/// Frame: [ length(4, LE) | tag(16) | enc(body) ].
/// Strictly one instance per connection side; stateful and not thread-safe. On any
/// <see cref="WireDecodeStatus.Corrupt"/> the connection must be torn down — the stream
/// cannot be resynchronized.
/// </summary>
public sealed class CounterAesGcmWireCodec : IWireCodec, IDisposable
{
    /// <summary>
    /// Creates a codec for one side of a single connection.
    /// </summary>
    /// <param name="key">Exactly 32 bytes (AES-256).</param>
    /// <param name="direction">
    /// The direction this side sends in (<see cref="WireDirection.ClientToServer"/> on the
    /// client, <see cref="WireDirection.ServerToClient"/> on the server). The receive
    /// direction is inferred as the opposite one.
    /// </param>
    /// <param name="maxMessageLength">Body length limit (1 MB by default) — a guard against reallocations driven by a garbage length.</param>
    public CounterAesGcmWireCodec(byte[] key, WireDirection direction, int maxMessageLength = 1024 * 1024)
    {
        ThrowHelper.ThrowIfNull(key);
        ThrowHelper.ThrowIfNegativeOrZero(maxMessageLength);

        // Explicit, consistent contract: fail with an Argument* exception and a clear
        // message instead of the platform's CryptographicException.
        if (key.Length is not KEY_SIZE) { throw new ArgumentException("Key must be exactly 32 bytes (AES-256).", nameof(key)); }
        if (direction is not (WireDirection.ClientToServer or WireDirection.ServerToClient)) { throw new ArgumentOutOfRangeException(nameof(direction)); }

#if NETSTANDARD2_1
        // netstandard2.1 exposes only AesGcm(key); the tag size is taken from the length of
        // the tag span at Encrypt/Decrypt time (always TAG_SIZE here), so the wire format is
        // identical to the modern two-argument constructor.
        _aes = new AesGcm(key);
#else
        _aes = new AesGcm(key, TAG_SIZE);
#endif
        _maxMessageLength = maxMessageLength;
        _sendDirection = (uint)direction;
        _receiveDirection = direction == WireDirection.ClientToServer
            ? (uint)WireDirection.ServerToClient
            : (uint)WireDirection.ClientToServer;
    }

    /// <inheritdoc/>
    public byte[] Encode(ReadOnlySpan<byte> plaintext)
    {
        // 2^64 frames is unreachable in practice; fail loudly rather than reuse a nonce.
        if (_sendCounter == ulong.MaxValue) { throw new InvalidOperationException("Send counter exhausted — reconnect (and rekey) required."); }

        var result = new byte[FRAME_OVERHEAD + plaintext.Length];

        // The length is in clear text, but it also becomes the AAD for the body.
        var lengthField = result.AsSpan(0, LENGTH_PREFIX_SIZE);
        BinaryPrimitives.WriteInt32LittleEndian(lengthField, plaintext.Length);

        var tag = result.AsSpan(LENGTH_PREFIX_SIZE, TAG_SIZE);
        var cipher = result.AsSpan(FRAME_OVERHEAD);

        Span<byte> nonce = stackalloc byte[NONCE_SIZE];
        FillNonce(nonce, _sendDirection, _sendCounter);

        _aes.Encrypt(nonce, plaintext, cipher, tag, lengthField);

        // Advance only after a successful encrypt so a thrown exception leaves state intact.
        _sendCounter++;

        return result;
    }

    /// <inheritdoc/>
    public WireDecodeStatus TryDecode(byte[] buffer, int offset, int count, out byte[]? plaintext, out int consumed)
    {
        ThrowHelper.ThrowIfNull(buffer);
        if (offset < 0 || count < 0 || offset + count > buffer.Length) { throw new ArgumentOutOfRangeException(nameof(count)); }

        plaintext = default;
        consumed = 0;

        // Wait for at least the length prefix.
        if (count < LENGTH_PREFIX_SIZE) { return WireDecodeStatus.NeedMoreData; }

        var length = BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(offset, LENGTH_PREFIX_SIZE));
        if (length < 0 || length > _maxMessageLength) { return WireDecodeStatus.Corrupt; }

        var total = FRAME_OVERHEAD + length;
        if (count < total) { return WireDecodeStatus.NeedMoreData; }

        // The length takes part in the tag check as AAD — tampering is caught right here.
        ReadOnlySpan<byte> lengthField = buffer.AsSpan(offset, LENGTH_PREFIX_SIZE);
        ReadOnlySpan<byte> tag = buffer.AsSpan(offset + LENGTH_PREFIX_SIZE, TAG_SIZE);
        ReadOnlySpan<byte> cipher = buffer.AsSpan(offset + FRAME_OVERHEAD, length);

        // The expected nonce is derived, not read from the wire: a replayed or reordered
        // frame was sealed under a different counter and fails the tag check below.
        Span<byte> nonce = stackalloc byte[NONCE_SIZE];
        FillNonce(nonce, _receiveDirection, _receiveExpected);

        var body = new byte[length];
        try
        {
            _aes.Decrypt(nonce, cipher, tag, body, lengthField);
        }
        catch (CryptographicException)
        {
            return WireDecodeStatus.Corrupt;
        }

        // Advance only after a verified frame; NeedMoreData/Corrupt never move the counter.
        _receiveExpected++;

        plaintext = body;
        consumed = total;
        return WireDecodeStatus.Ok;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _aes.Dispose();
    }

    private static void FillNonce(Span<byte> nonce, uint direction, ulong counter)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(nonce, direction);
        BinaryPrimitives.WriteUInt64LittleEndian(nonce[sizeof(uint)..], counter);
    }

    /// <summary>The only supported key size: 32 bytes (AES-256).</summary>
    internal const int KEY_SIZE = 32;

    /// <summary>Nonce size (derived on both sides, never on the wire).</summary>
    internal const int NONCE_SIZE = 12;

    /// <summary>Tag size.</summary>
    internal const int TAG_SIZE = 16;

    /// <summary>Open (but authenticated) body length prefix.</summary>
    internal const int LENGTH_PREFIX_SIZE = sizeof(int);

    /// <summary>Total frame overhead on top of the body length (no nonce on the wire).</summary>
    internal const int FRAME_OVERHEAD = LENGTH_PREFIX_SIZE + TAG_SIZE;

    private readonly AesGcm _aes;
    private readonly int _maxMessageLength;
    private readonly uint _sendDirection;
    private readonly uint _receiveDirection;
    private ulong _sendCounter;
    private ulong _receiveExpected;
}