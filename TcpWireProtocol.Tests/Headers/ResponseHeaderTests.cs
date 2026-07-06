using System;
using System.Buffers.Binary;
using NUnit.Framework;
using TcpWireProtocol.Headers;

namespace TcpWireProtocol.Tests.Headers;

public class ResponseHeaderTests
{
    /// <summary>
    /// Negative payload length
    /// </summary>
    [Test]
    public void Constructor_ArgumentOutOfRangeException_PayloadLength()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ResponseHeader(1, -10));
    }

    /// <summary>
    /// Header data is null
    /// </summary>
    [Test]
    public void TryParse_ArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ResponseHeader.TryParse(null!, out var header));
    }

    /// <summary>
    /// Enough data for the header. Payload present
    /// </summary>
    [Test]
    public void TryParse_EnoughDataWithPayload()
    {
        const int CMD_ID = 3151;
        const int PAYLOAD_LENGTH = 812;
        var tmpHeader = new ResponseHeader(CMD_ID, PAYLOAD_LENGTH);

        var result = ResponseHeader.TryParse(tmpHeader.RawBytes.ToArray(), out var header);

        Assert.IsTrue(result);
        Assert.IsNotNull(header);
        CollectionAssert.AreEqual(tmpHeader.RawBytes.ToArray(), header!.RawBytes.ToArray());
        Assert.AreEqual(CMD_ID, header.MainHeader.CmdId);
        Assert.AreEqual(PAYLOAD_LENGTH, header.MainHeader.PayloadLength);
    }

    /// <summary>
    /// Enough data for the header. Payload absent
    /// </summary>
    [Test]
    public void TryParse_EnoughDataWithoutPayload()
    {
        const int CMD_ID = 3151;
        const int PAYLOAD_LENGTH = 0;

        var tmpHeader = new ResponseHeader(CMD_ID, PAYLOAD_LENGTH);

        var result = ResponseHeader.TryParse(tmpHeader.RawBytes.ToArray(), out var header);

        Assert.IsTrue(result);
        Assert.IsNotNull(header);
        CollectionAssert.AreEqual(tmpHeader.RawBytes.ToArray(), header!.RawBytes.ToArray());
        Assert.AreEqual(CMD_ID, header.MainHeader.CmdId);
        Assert.AreEqual(PAYLOAD_LENGTH, header.MainHeader.PayloadLength);
    }

    /// <summary>
    /// Not enough data for the header
    /// </summary>
    [Test]
    public void TryParse_NotEnoughData()
    {
        var result = ResponseHeader.TryParse([10, 20, 30], out var header);

        Assert.IsFalse(result);
        Assert.IsNull(header);
    }

    /// <summary>
    /// A negative (garbage/hostile) length field must fail parsing, not throw.
    /// </summary>
    [Test]
    public void TryParse_NegativeLength_ReturnsFalse()
    {
        var data = new ResponseHeader(1, 0).RawBytes.ToArray();
        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(sizeof(int)), -1);

        var result = false;
        Assert.DoesNotThrow(() => result = ResponseHeader.TryParse(data, out _));
        Assert.IsFalse(result);
    }
}