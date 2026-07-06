using System;
using System.Buffers.Binary;

using NUnit.Framework;
using TcpWireProtocol.Headers;

namespace TcpWireProtocol.Tests.Headers;

public class EventHeaderTests
{
    /// <summary>
    /// Negative payload length
    /// </summary>
    [Test]
    public void Constructor_ArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new EventHeader(10, 10, -10));
    }

    /// <summary>
    /// Header data is null
    /// </summary>
    [Test]
    public void TryParse_ArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => EventHeader.TryParse(null!, out var header));
    }

    /// <summary>
    /// Verify the payload-length hack
    /// </summary>
    [Test]
    public void Constructor_CreateEvent()
    {
        const int PAYLOAD_LENGTH = 10;

        var tmpEvt = new EventHeader(10, 12, PAYLOAD_LENGTH);
        var tmpResponse = new ResponseHeader(1, PAYLOAD_LENGTH);

        Assert.AreEqual(tmpEvt.MainHeader.CmdId, 0);
        Assert.AreEqual(tmpEvt.MainHeader.PayloadLength, tmpResponse.MainHeader.PayloadLength);
    }

    /// <summary>
    /// Enough data for the header. Payload present
    /// </summary>
    [Test]
    public void TryParse_EnoughDataWithPayload()
    {
        const short SERVICE = 10;
        const short COMMAND = 12;
        const int PAYLOAD_LENGTH = 312;

        var tmpHeader = new EventHeader(SERVICE, COMMAND, PAYLOAD_LENGTH);

        var result = EventHeader.TryParse(tmpHeader.RawBytes.ToArray(), out var header);

        Assert.IsTrue(result);
        Assert.IsNotNull(header);
        CollectionAssert.AreEqual(tmpHeader.RawBytes.ToArray(), header!.RawBytes.ToArray());
        Assert.AreEqual(PAYLOAD_LENGTH, header.MainHeader.PayloadLength);
    }

    /// <summary>
    /// An event header must be readable the same way as a response header
    /// </summary>
    [Test]
    public void TryParse_AsResponseHeader()
    {
        const short SERVICE = 10;
        const short COMMAND = 12;
        const int PAYLOAD_LENGTH = 312;

        var tmpHeader = new EventHeader(SERVICE, COMMAND, PAYLOAD_LENGTH);

        var result = ResponseHeader.TryParse(tmpHeader.RawBytes.ToArray(), out var response);

        Assert.IsTrue(result);
        Assert.IsNotNull(response);

        // When reading an event as a response, its service part is counted into the payload.
        Assert.AreEqual(tmpHeader.MainHeader.PayloadLength + ServiceHeader.HEADER_LENGTH, response!.MainHeader.PayloadLength);
        Assert.AreEqual(tmpHeader.RawBytes.Length, response.RawBytes.Length + ServiceHeader.HEADER_LENGTH);
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

        var tmpHeader = new EventHeader(SERVICE, COMMAND, PAYLOAD_LENGTH);

        var result = EventHeader.TryParse(tmpHeader.RawBytes.ToArray(), out var header);

        Assert.IsTrue(result);
        Assert.IsNotNull(header);
        CollectionAssert.AreEqual(tmpHeader.RawBytes.ToArray(), header!.RawBytes.ToArray());
        Assert.AreEqual(PAYLOAD_LENGTH, header.MainHeader.PayloadLength);
    }

    /// <summary>
    /// Not enough data for the header
    /// </summary>
    [Test]
    public void TryParse_NotEnoughData()
    {
        var result = EventHeader.TryParse([10, 20, 30], out var header);

        Assert.IsFalse(result);
        Assert.IsNull(header);
    }

    /// <summary>
    /// A wire length below the service part is impossible for a real event: it must
    /// fail parsing, not throw a negative payload length from the constructor.
    /// </summary>
    [Test]
    public void TryParse_WireLengthBelowServicePart_ReturnsFalse()
    {
        var data = new EventHeader(10, 12, 0).RawBytes.ToArray();
        // Real events count the service part (4) into the length; anything smaller is garbage.
        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(sizeof(int)), ServiceHeader.HEADER_LENGTH - 1);

        var result = false;
        Assert.DoesNotThrow(() => result = EventHeader.TryParse(data, out _));
        Assert.IsFalse(result);
    }
}