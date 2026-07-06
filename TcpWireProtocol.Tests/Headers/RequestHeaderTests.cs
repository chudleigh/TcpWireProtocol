using System;
using System.Buffers.Binary;

using NUnit.Framework;
using TcpWireProtocol.Headers;

namespace TcpWireProtocol.Tests.Headers;

public class RequestHeaderTests
{
    /// <summary>
    /// Negative payload length
    /// </summary>
    [Test]
    public void Constructor_ArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RequestHeader(10, 10, -10));
    }

    /// <summary>
    /// Header data is null
    /// </summary>
    [Test]
    public void TryParse_ArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => RequestHeader.TryParse(null!, out var header));
    }

    /// <summary>
    /// Enough data for the header. Payload present
    /// </summary>
    [Test]
    public void TryParse_EnoughDataWithPayload()
    {
        const short SERVICE = 10;
        const short COMMAND = 12;
        var payload = new byte[] { 10, 20, 30 };

        var tmpHeader = new RequestHeader(SERVICE, COMMAND, payload.Length);

        var result = RequestHeader.TryParse(tmpHeader.RawBytes.ToArray(), out var header);

        Assert.IsTrue(result);
        Assert.IsNotNull(header);
        CollectionAssert.AreEqual(tmpHeader.RawBytes.ToArray(), header!.RawBytes.ToArray());
        Assert.AreEqual(SERVICE, header.ServiceHeader.Service);
        Assert.AreEqual(COMMAND, header.ServiceHeader.Command);
        Assert.AreEqual(tmpHeader.MainHeader.CmdId, header.MainHeader.CmdId);
        Assert.AreEqual(tmpHeader.MainHeader.PayloadLength, header.MainHeader.PayloadLength);
        Assert.AreEqual(payload.Length, header.MainHeader.PayloadLength);
    }

    /// <summary>
    /// Enough data for the header. Payload absent
    /// </summary>
    [Test]
    public void TryParse_EnoughDataWithoutPayload()
    {
        const short SERVICE = 10;
        const short COMMAND = 12;
        const int PAYLOAD_LENGTH = 0;

        var tmpHeader = new RequestHeader(SERVICE, COMMAND, PAYLOAD_LENGTH);

        var result = RequestHeader.TryParse(tmpHeader.RawBytes.ToArray(), out var header);

        Assert.IsTrue(result);
        Assert.IsNotNull(header);
        CollectionAssert.AreEqual(tmpHeader.RawBytes.ToArray(), header!.RawBytes.ToArray());
        Assert.AreEqual(SERVICE, header.ServiceHeader.Service);
        Assert.AreEqual(COMMAND, header.ServiceHeader.Command);
        Assert.AreEqual(tmpHeader.MainHeader.CmdId, header.MainHeader.CmdId);
        Assert.AreEqual(tmpHeader.MainHeader.PayloadLength, header.MainHeader.PayloadLength);
        Assert.AreEqual(PAYLOAD_LENGTH, header.MainHeader.PayloadLength);
    }

    /// <summary>
    /// Not enough data for the header
    /// </summary>
    [Test]
    public void TryParse_NotEnoughData()
    {
        var result = RequestHeader.TryParse([10, 20, 30], out var header);

        Assert.IsFalse(result);
        Assert.IsNull(header);
    }

    /// <summary>
    /// A negative (garbage/hostile) length field must fail parsing, not throw.
    /// </summary>
    [Test]
    public void TryParse_NegativeLength_ReturnsFalse()
    {
        var data = new RequestHeader(10, 12, 0).RawBytes.ToArray();
        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(sizeof(int)), -1);

        var result = false;
        Assert.DoesNotThrow(() => result = RequestHeader.TryParse(data, out _));
        Assert.IsFalse(result);
    }
}