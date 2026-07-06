using System;
using System.Diagnostics.CodeAnalysis;
using TcpWireProtocol.Headers;

namespace TcpWireProtocol.Packets;

/// <summary>
/// Message sent on the server's initiative.
/// </summary>
public sealed class Event : Packet<EventHeader>
{
    /// <summary>
    /// Creates an event for the given service and command with an optional payload.
    /// </summary>
    public Event(short service, short command, byte[]? payload = default) :
        this(new EventHeader(service, command, payload?.Length ?? 0), payload)
    {
    }

    private Event(EventHeader header, byte[]? payload = default)
            : base(header, payload)
    {
    }

    /// <summary>
    /// Parses a packet from raw data.
    /// </summary>
    public static bool TryParse(byte[] data, [NotNullWhen(true)] out Event? evt)
    {
        ThrowHelper.ThrowIfNull(data);

        // Assign the default value.
        evt = default;

        // If the header could be parsed and there is enough data for the whole packet.
        if (EventHeader.TryParse(data, out var header) && data.Length - EventHeader.HEADER_LENGTH >= header.MainHeader.PayloadLength)
        {
            var payload = new byte[header.MainHeader.PayloadLength];
            Array.Copy(data, EventHeader.HEADER_LENGTH, payload, 0, header.MainHeader.PayloadLength);

            evt = new Event(header, header.MainHeader.PayloadLength > 0 ? payload : default);
            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        var service = Header.ServiceHeader;
        return $"Event svc={service.Service} cmd={service.Command} payload={Payload?.Length ?? 0}B";
    }
}