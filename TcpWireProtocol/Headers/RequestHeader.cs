using System;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using TcpWireProtocol.Interfaces;

namespace TcpWireProtocol.Headers;

/// <summary>
/// Request header: cmdId + payload length + service part. Immutable.
/// </summary>
public sealed class RequestHeader : IHeader
{
    /// <inheritdoc/>
    public MainHeader MainHeader { get; }

    /// <inheritdoc/>
    public ServiceHeader ServiceHeader { get; }

    /// <inheritdoc/>
    public ReadOnlyMemory<byte> RawBytes => _rawBytes;

    /// <summary>
    /// Creates a request header with an auto-incremented cmdId.
    /// </summary>
    public RequestHeader(short service, short command, int payloadLength)
        : this(NextCmdId(), service, command, payloadLength)
    {
    }

    private RequestHeader(int cmdId, short service, short command, int payloadLength)
    {
        ThrowHelper.ThrowIfNegative(payloadLength);

        MainHeader = new MainHeader(cmdId, payloadLength);
        ServiceHeader = new ServiceHeader(service, command);

        var buffer = new byte[HEADER_LENGTH];
        BinaryPrimitives.WriteInt32LittleEndian(buffer, MainHeader.CmdId);
        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(sizeof(int)), MainHeader.PayloadLength);
        BinaryPrimitives.WriteInt16LittleEndian(buffer.AsSpan(MainHeader.HEADER_LENGTH), ServiceHeader.Service);
        BinaryPrimitives.WriteInt16LittleEndian(buffer.AsSpan(MainHeader.HEADER_LENGTH + sizeof(short)), ServiceHeader.Command);
        _rawBytes = buffer;
    }

    /// <summary>
    /// Tries to parse a request header from a buffer.
    /// </summary>
    public static bool TryParse(byte[] data, [NotNullWhen(true)] out RequestHeader? header)
    {
        ThrowHelper.ThrowIfNull(data);

        header = default;
        if (data.Length < HEADER_LENGTH) { return false; }

        var cmdId = BinaryPrimitives.ReadInt32LittleEndian(data);
        var length = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(sizeof(int)));
        var service = BinaryPrimitives.ReadInt16LittleEndian(data.AsSpan(MainHeader.HEADER_LENGTH));
        var command = BinaryPrimitives.ReadInt16LittleEndian(data.AsSpan(MainHeader.HEADER_LENGTH + sizeof(short)));

        // A garbage/hostile length must fail parsing, not throw from the constructor.
        if (length < 0) { return false; }

        header = new RequestHeader(cmdId, service, command, length);
        return true;
    }

    /// <summary>
    /// Thread-safely returns the next non-zero request id
    /// (cmdId == 0 is reserved for events).
    /// </summary>
    private static int NextCmdId()
    {
        int cmdId;
        do
        {
            // Interlocked.Increment wraps around without throwing on overflow.
            cmdId = Interlocked.Increment(ref _cmdId);
        }
        while (cmdId == 0);

        return cmdId;
    }

    /// <summary>Header size in bytes.</summary>
    public const int HEADER_LENGTH = MainHeader.HEADER_LENGTH + ServiceHeader.HEADER_LENGTH;

    /// <summary>Auto-incremented field used to generate a unique cmdId.</summary>
    private static int _cmdId;

    private readonly byte[] _rawBytes;
}