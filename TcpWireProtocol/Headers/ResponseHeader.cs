using System;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using TcpWireProtocol.Interfaces;

namespace TcpWireProtocol.Headers;

/// <summary>
/// Response header: cmdId + payload length. Immutable after creation.
/// </summary>
public class ResponseHeader : IHeader
{
    /// <inheritdoc/>
    public MainHeader MainHeader { get; }

    /// <inheritdoc/>
    public ServiceHeader? ServiceHeader { get; }

    /// <inheritdoc/>
    public ReadOnlyMemory<byte> RawBytes => _rawBytes;

    /// <summary>
    /// Creates a response header.
    /// </summary>
    public ResponseHeader(int cmdId, int payloadLength)
    {
        ThrowHelper.ThrowIfNegative(payloadLength);

        MainHeader = new MainHeader(cmdId, payloadLength);

        var buffer = new byte[HEADER_LENGTH];
        BinaryPrimitives.WriteInt32LittleEndian(buffer, MainHeader.CmdId);
        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(sizeof(int)), MainHeader.PayloadLength);
        _rawBytes = buffer;
    }

    /// <summary>
    /// Constructor for derived types: the header is already built by external code.
    /// </summary>
    protected ResponseHeader(MainHeader mainHeader, ServiceHeader serviceHeader, byte[] rawBytes)
    {
        MainHeader = mainHeader;
        ServiceHeader = serviceHeader;
        _rawBytes = rawBytes;
    }

    /// <summary>
    /// Tries to parse an response header from a buffer.
    /// </summary>
    public static bool TryParse(byte[] data, [NotNullWhen(true)] out ResponseHeader? header)
    {
        ThrowHelper.ThrowIfNull(data);

        header = default;
        if (data.Length < HEADER_LENGTH) { return false; }

        var cmdId = BinaryPrimitives.ReadInt32LittleEndian(data);
        var length = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(sizeof(int)));

        // A garbage/hostile length must fail parsing, not throw from the constructor.
        if (length < 0) { return false; }

        header = new ResponseHeader(cmdId, length);
        return true;
    }

    /// <summary>Header size in bytes.</summary>
    public const int HEADER_LENGTH = MainHeader.HEADER_LENGTH;

    private readonly byte[] _rawBytes;
}