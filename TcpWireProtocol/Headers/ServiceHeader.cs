namespace TcpWireProtocol.Headers;

/// <summary>
/// Service part of the header: service and command (request routing).
/// </summary>
public sealed class ServiceHeader(short service, short command)
{
    /// <summary>Service responsible for handling the request.</summary>
    public short Service { get; } = service;

    /// <summary>Command that selects the request handler within the service.</summary>
    public short Command { get; } = command;

    /// <summary>Header size in bytes.</summary>
    public const int HEADER_LENGTH = sizeof(short) + sizeof(short);
}