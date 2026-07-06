using System;
using System.Diagnostics.CodeAnalysis;
using TcpWireProtocol.Headers;

namespace TcpWireProtocol.Packets;

/// <summary>
/// Request sent by the client to the server.
/// </summary>
public sealed class Request : Packet<RequestHeader>
{
    /// <summary>
    /// Creates a request for the given service and command with an optional payload.
    /// </summary>
    public Request(short service, short command, byte[]? payload = default) :
        this(new RequestHeader(service, command, payload?.Length ?? 0), payload)
    {
    }

    private Request(RequestHeader header, byte[]? payload = default)
            : base(header, payload)
    {
    }

    /// <summary>
    /// Parses a packet from raw data.
    /// </summary>
    public static bool TryParse(byte[] data, [NotNullWhen(true)] out Request? request)
    {
        ThrowHelper.ThrowIfNull(data);

        // Assign the default value.
        request = default;

        // If the header could be parsed and there is enough data for the whole packet.
        if (RequestHeader.TryParse(data, out var header) && data.Length - RequestHeader.HEADER_LENGTH >= header.MainHeader.PayloadLength)
        {
            // Read the payload.
            var payload = new byte[header.MainHeader.PayloadLength];
            Array.Copy(data, RequestHeader.HEADER_LENGTH, payload, 0, header.MainHeader.PayloadLength);

            // Build the packet.
            request = new Request(header, header.MainHeader.PayloadLength > 0 ? payload : default);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Creates a reply to this request.
    /// </summary>
    public Response CreateResponse(byte[]? payload = default)
    {
        return new Response(Header.MainHeader.CmdId, payload);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"Request #{Header.MainHeader.CmdId} svc={Header.ServiceHeader.Service} cmd={Header.ServiceHeader.Command} payload={Payload?.Length ?? 0}B";
    }
}