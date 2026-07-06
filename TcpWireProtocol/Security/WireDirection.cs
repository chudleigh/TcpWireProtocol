namespace TcpWireProtocol.Security;

/// <summary>
/// Traffic direction of a connection side. Used as the nonce prefix by
/// <see cref="CounterAesGcmWireCodec"/> so that both sides can share one key
/// without ever producing the same (key, nonce) pair.
/// </summary>
public enum WireDirection : uint
{
    /// <summary>Frames sent by the client to the server.</summary>
    ClientToServer = 1,

    /// <summary>Frames sent by the server to the client.</summary>
    ServerToClient = 2,
}