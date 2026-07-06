namespace TcpWireProtocol.Headers;

/// <summary>
/// Main header: request id and payload length.
/// </summary>
public sealed class MainHeader(int cmdId, int payloadLength)
{
    /// <summary>Request id.</summary>
    public int CmdId { get; } = cmdId;

    /// <summary>Payload length.</summary>
    public int PayloadLength { get; } = payloadLength;

    /// <summary>Header size in bytes.</summary>
    public const int HEADER_LENGTH = sizeof(int) + sizeof(int);
}