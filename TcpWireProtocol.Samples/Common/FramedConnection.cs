using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using TcpWireProtocol.Interfaces;
using TcpWireProtocol.Security;

namespace TcpWireProtocol.Samples.Common;

/// <summary>
/// The bridge between a raw <see cref="NetworkStream"/> and an <see cref="IWireCodec"/>: writes
/// packets as framed messages and reassembles incoming bytes back into whole decrypted messages.
/// The one place the TCP accumulation loop lives, so callers work in whole messages.
/// One instance per connection (the codec is stateful), not thread-safe.
/// </summary>
public sealed class FramedConnection : IDisposable
{
    /// <summary>
    /// Wraps an already-connected stream with a codec. Prefer the <see cref="Server"/> /
    /// <see cref="Client"/> factories, which pick the sending direction for you.
    /// </summary>
    public FramedConnection(NetworkStream stream, IWireCodec codec, int maxMessage = 1024 * 1024)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(codec);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxMessage);

        _stream = stream;
        _codec = codec;
        _maxMessage = maxMessage;
    }

    /// <summary>Wraps the server side of a connection (sends <c>ServerToClient</c>).</summary>
    public static FramedConnection Server(NetworkStream stream, byte[]? key = null)
    {
        return new FramedConnection(stream, WireCodec.Create(WireDirection.ServerToClient, key));
    }

    /// <summary>Wraps the client side of a connection (sends <c>ClientToServer</c>).</summary>
    public static FramedConnection Client(NetworkStream stream, byte[]? key = null)
    {
        return new FramedConnection(stream, WireCodec.Create(WireDirection.ClientToServer, key));
    }

    /// <summary>Encodes and writes one packet as a single self-delimiting frame.</summary>
    public async Task SendAsync<THeader>(IPacket<THeader> packet, CancellationToken ct = default)
        where THeader : IHeader
    {
        var frame = _codec.Encode(packet);
        await _stream.WriteAsync(frame, ct);
    }

    /// <summary>Returns the next whole decrypted message, or <c>null</c> on a clean close.</summary>
    public async Task<byte[]?> ReceiveAsync(CancellationToken ct = default)
    {
        while (true)
        {
            if (_length > 0)
            {
                var status = _codec.TryDecode(_buffer, _start, _length, out var message, out var consumed);
                if (status == WireDecodeStatus.Ok)
                {
                    _start += consumed;
                    _length -= consumed;
                    return message;
                }

                if (status == WireDecodeStatus.Corrupt)
                {
                    throw new InvalidDataException("frame corrupt — wrong key, replay, or tampering");
                }

                // NeedMoreData: fall through and pull more bytes off the socket.
            }

            MakeRoom();

            var read = await _stream.ReadAsync(_buffer.AsMemory(_start + _length), ct);
            if (read == 0)
            {
                if (_length == 0) { return null; }
                throw new EndOfStreamException("connection closed in the middle of a frame");
            }

            _length += read;
        }
    }

    /// <summary>Yields whole decrypted messages until the connection closes.</summary>
    public async IAsyncEnumerable<byte[]> ReceiveAllAsync([EnumeratorCancellation] CancellationToken ct = default)
    {
        byte[]? message;
        while ((message = await ReceiveAsync(ct)) is not null)
        {
            yield return message;
        }
    }

    /// <summary>
    /// Swaps the codec (for example after a key exchange). The new codec restarts its counter at 0,
    /// so treat rekeying as a barrier: run the key-exchange round-trip with no other request in
    /// flight, and call this at a clean frame boundary (nothing partially received). A frame that
    /// crosses the switch under the wrong codec fails the tag check as
    /// <see cref="WireDecodeStatus.Corrupt"/>. The old codec is disposed.
    /// </summary>
    public void Rekey(IWireCodec codec)
    {
        ArgumentNullException.ThrowIfNull(codec);
        if (_length != 0) { throw new InvalidOperationException("cannot rekey with a partial frame buffered"); }

        (_codec as IDisposable)?.Dispose();
        _codec = codec;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        (_codec as IDisposable)?.Dispose();
        _stream.Dispose();
    }

    /// <summary>
    /// Ensures there is free space at the tail to read into. Only rearranges when the tail is
    /// exhausted: first reclaims the already-consumed head, then grows the buffer if a single
    /// frame still won't fit.
    /// </summary>
    private void MakeRoom()
    {
        if (_start + _length < _buffer.Length) { return; }   // room at the tail — nothing to do

        if (_start > 0)
        {
            Array.Copy(_buffer, _start, _buffer, 0, _length);   // slide the live window to the front
            _start = 0;
        }

        if (_length == _buffer.Length)
        {
            var size = Math.Min(_buffer.Length * 2, _maxMessage + FRAME_HEADROOM);
            Array.Resize(ref _buffer, size);
        }
    }

    /// <summary>Starting buffer size; grows on demand only if a single frame is larger.</summary>
    private const int INITIAL_CAPACITY = 4096;

    /// <summary>Headroom over the max body so a grown buffer always holds one whole frame (length prefix + tag + body).</summary>
    private const int FRAME_HEADROOM = 64;

    private readonly NetworkStream _stream;
    private readonly int _maxMessage;
    private IWireCodec _codec;
    private byte[] _buffer = new byte[INITIAL_CAPACITY];
    private int _start;    // offset of the first unconsumed byte
    private int _length;   // number of unconsumed bytes
}