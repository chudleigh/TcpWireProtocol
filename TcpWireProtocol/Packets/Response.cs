using System;
using System.Diagnostics.CodeAnalysis;
using TcpWireProtocol.Headers;

namespace TcpWireProtocol.Packets;

/// <summary>
/// Reply to a client request.
/// </summary>
public sealed class Response : Packet<ResponseHeader>
{
    /// <summary>
    /// Whether this is an event: events arrive on the reply channel with a reserved cmdId == 0.
    /// </summary>
    public bool IsEvent => Header.MainHeader.CmdId == 0;

    /// <summary>
    /// Creates a reply for the given request id with an optional payload.
    /// </summary>
    public Response(int cmdId, byte[]? payload = default) :
        this(new ResponseHeader(cmdId, payload?.Length ?? 0), payload)
    {
    }

    private Response(ResponseHeader header, byte[]? payload = default)
        : base(header, payload)
    {
    }

    /// <summary>
    /// Parses a packet from raw data.
    /// </summary>
    public static bool TryParse(byte[] data, [NotNullWhen(true)] out Response? response)
    {
        ThrowHelper.ThrowIfNull(data);

        // Assign the default value.
        response = default;

        // If the header could be parsed and there is enough data for the whole packet.
        if (ResponseHeader.TryParse(data, out var header) && data.Length - ResponseHeader.HEADER_LENGTH >= header.MainHeader.PayloadLength)
        {
            var payload = new byte[header.MainHeader.PayloadLength];
            Array.Copy(data, ResponseHeader.HEADER_LENGTH, payload, 0, header.MainHeader.PayloadLength);

            response = new Response(header, header.MainHeader.PayloadLength > 0 ? payload : default);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Tries to read this packet as an event. Succeeds only when <see cref="IsEvent"/>.
    /// </summary>
    public bool TryReadEvent([NotNullWhen(true)] out Event? evt)
    {
        if (!IsEvent)
        {
            evt = default;
            return false;
        }

        // Re-parse our own already-materialized buffer directly (no copy).
        return Event.TryParse(_rawBytes, out evt);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"Response #{Header.MainHeader.CmdId} payload={Payload?.Length ?? 0}B";
    }
}