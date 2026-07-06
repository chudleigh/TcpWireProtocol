namespace TcpWireProtocol.Samples.DiffieHellman.Shared;

/// <summary>Service and command identifiers for the Diffie-Hellman sample.</summary>
public static class Protocol
{
    /// <summary>The single application service.</summary>
    public const short APP = 1;

    /// <summary>Echo the payload back.</summary>
    public const short ECHO = 1;

    /// <summary>Agree a shared key (the first exchange is the handshake, later ones are rekeys).</summary>
    public const short KEY_EXCHANGE = 2;
}