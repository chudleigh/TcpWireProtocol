using System.Diagnostics.CodeAnalysis;
using TcpWireProtocol.Interfaces;

namespace TcpWireProtocol.Security;

/// <summary>
/// Convenience wrappers over <see cref="IWireCodec"/>.
/// </summary>
public static class WireCodecExtensions
{
    /// <summary>Pack a packet (== <c>codec.Encode(packet.RawBytes.Span)</c>).</summary>
    public static byte[] Encode<THeader>(this IWireCodec codec, IPacket<THeader> packet)
        where THeader : IHeader
    {
        ThrowHelper.ThrowIfNull(codec);
        ThrowHelper.ThrowIfNull(packet);

        return codec.Encode(packet.RawBytes.Span);
    }

    /// <summary>
    /// Parse when <paramref name="wire"/> holds exactly one full message.
    /// For a stream, use <see cref="IWireCodec.TryDecode"/> directly.
    /// </summary>
    public static bool TryDecode(this IWireCodec codec, byte[] wire, [NotNullWhen(true)] out byte[]? plaintext)
    {
        ThrowHelper.ThrowIfNull(codec);
        ThrowHelper.ThrowIfNull(wire);

        return codec.TryDecode(wire, 0, wire.Length, out plaintext, out _) == WireDecodeStatus.Ok;
    }
}