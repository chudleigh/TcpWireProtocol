using System;
using TcpWireProtocol.Interfaces;

namespace TcpWireProtocol.Packets;

/// <summary>
/// Base packet: header + payload glued into a single buffer.
/// </summary>
public abstract class Packet<THeader> : IPacket<THeader>
    where THeader : IHeader
{
    /// <inheritdoc/>
    public THeader Header { get; }

    /// <inheritdoc/>
    public byte[]? Payload { get; }

    /// <inheritdoc/>
    public ReadOnlyMemory<byte> RawBytes => _rawBytes;

    /// <summary>
    /// Initializes the packet from a header and an optional payload.
    /// </summary>
    protected Packet(THeader header, byte[]? payload)
    {
        Header = header;
        Payload = payload;

        var headerBuffer = header.RawBytes;
        var buffer = new byte[headerBuffer.Length + (payload?.Length ?? 0)];
        headerBuffer.Span.CopyTo(buffer);
        payload?.CopyTo(buffer, headerBuffer.Length);
        _rawBytes = buffer;
    }

    /// <summary>Backing store for the serialized bytes; shared read-only with derived types.</summary>
    protected readonly byte[] _rawBytes;
}