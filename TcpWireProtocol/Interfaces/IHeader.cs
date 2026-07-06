using System;
using TcpWireProtocol.Headers;

namespace TcpWireProtocol.Interfaces;

/// <summary>
/// Packet header.
/// </summary>
public interface IHeader
{
    /// <summary>
    /// Main header of the packet.
    /// </summary>
    MainHeader MainHeader { get; }

    /// <summary>
    /// Service header (null for a response — it carries no service part).
    /// </summary>
    ServiceHeader? ServiceHeader { get; }

    /// <summary>
    /// Raw header bytes (a read-only view over the internal buffer; zero-copy).
    /// </summary>
    ReadOnlyMemory<byte> RawBytes { get; }
}