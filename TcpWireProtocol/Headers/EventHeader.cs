using System;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;

namespace TcpWireProtocol.Headers;

/// <summary>
/// Event header: like an response, but the service part is "hidden" inside the body so the
/// layout matches an response. Immutable after creation.
/// </summary>
/// <remarks>
/// Creates an event header.
/// </remarks>
public sealed class EventHeader(short service, short command, int payloadLength) : ResponseHeader(new MainHeader(0, payloadLength), new ServiceHeader(service, command), BuildRawBytes(service, command, payloadLength))
{
    /// <summary>The service part, always present on an event (never null, unlike a plain response).</summary>
    public new ServiceHeader ServiceHeader => base.ServiceHeader!;

    /// <summary>
    /// Tries to parse an event header from a buffer.
    /// </summary>
    public static bool TryParse(byte[] data, [NotNullWhen(true)] out EventHeader? header)
    {
        ThrowHelper.ThrowIfNull(data);

        header = default;
        if (data.Length < HEADER_LENGTH) { return false; }

        // The on-wire length counts the service part into the body; a valid event
        // therefore never reports less than the service part. A smaller (or negative)
        // value is garbage and must fail parsing, not throw from the constructor.
        var wireLength = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(sizeof(int)));
        if (wireLength < ServiceHeader.HEADER_LENGTH) { return false; }

        // Compute the real payload length (without the service part).
        var length = wireLength - ServiceHeader.HEADER_LENGTH;
        var service = BinaryPrimitives.ReadInt16LittleEndian(data.AsSpan(MainHeader.HEADER_LENGTH));
        var command = BinaryPrimitives.ReadInt16LittleEndian(data.AsSpan(MainHeader.HEADER_LENGTH + sizeof(short)));

        header = new EventHeader(service, command, length);
        return true;
    }

    // cmdId == 0 is reserved for events; the service part is counted into the body length.
    private static byte[] BuildRawBytes(short service, short command, int payloadLength)
    {
        ThrowHelper.ThrowIfNegative(payloadLength);

        var buffer = new byte[HEADER_LENGTH];
        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(sizeof(int)), payloadLength + ServiceHeader.HEADER_LENGTH);
        BinaryPrimitives.WriteInt16LittleEndian(buffer.AsSpan(MainHeader.HEADER_LENGTH), service);
        BinaryPrimitives.WriteInt16LittleEndian(buffer.AsSpan(MainHeader.HEADER_LENGTH + sizeof(short)), command);
        return buffer;
    }

    /// <summary>Event header size in bytes (main header + service part).</summary>
    public new const int HEADER_LENGTH = ResponseHeader.HEADER_LENGTH + ServiceHeader.HEADER_LENGTH;
}