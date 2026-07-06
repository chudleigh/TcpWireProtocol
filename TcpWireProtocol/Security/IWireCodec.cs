using System;

namespace TcpWireProtocol.Security;

/// <summary>
/// Result of parsing a single message from the stream.
/// </summary>
public enum WireDecodeStatus
{
    /// <summary>Not enough data — accumulate more and retry.</summary>
    NeedMoreData = 0,

    /// <summary>Message received.</summary>
    Ok = 1,

    /// <summary>Data is corrupt / not authentic.</summary>
    Corrupt = 2,
}

/// <summary>
/// Codec: packs a packet's plaintext buffer into a "wire" message and parses it back.
/// Stateless — TCP-stream accumulation is done by the caller.
/// </summary>
public interface IWireCodec
{
    /// <summary>Pack a buffer into a self-delimiting message.</summary>
    byte[] Encode(ReadOnlySpan<byte> plaintext);

    /// <summary>
    /// Parse one message from <paramref name="buffer"/>[<paramref name="offset"/>..+<paramref name="count"/>].
    /// On <see cref="WireDecodeStatus.Ok"/>, <paramref name="consumed"/> is the frame length.
    /// </summary>
    WireDecodeStatus TryDecode(byte[] buffer, int offset, int count, out byte[]? plaintext, out int consumed);
}