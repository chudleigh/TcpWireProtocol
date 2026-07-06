using System;

namespace TcpWireProtocol.Interfaces;

/// <summary>
/// Packet.
/// </summary>
public interface IPacket<THeader>
    where THeader : IHeader
{
    /// <summary>
    /// Header.
    /// </summary>
    THeader Header { get; }

    /// <summary>
    /// Payload (null if there is none).
    /// </summary>
    byte[]? Payload { get; }

    /// <summary>
    /// Raw packet bytes (a read-only view over the internal buffer; zero-copy).
    /// </summary>
    ReadOnlyMemory<byte> RawBytes { get; }
}