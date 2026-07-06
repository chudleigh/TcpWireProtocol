using System;

using NUnit.Framework;
using TcpWireProtocol.Packets;

namespace TcpWireProtocol.Tests.Packets;

public class ResponseTests
{
    /// <summary>
    /// Packet data is null
    /// </summary>
    [Test]
    public void TryParse_ArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Response.TryParse(null!, out var response));
    }

    /// <summary>
    /// Enough data for the packet. Payload present
    /// </summary>
    [Test]
    public void TryParse_EnoughDataWithPayload()
    {
        const int CMD_ID = 3151;
        var payload = new byte[] { 10, 20, 30 };
        var tmpResponse = new Response(CMD_ID, payload);

        var result = Response.TryParse(tmpResponse.RawBytes.ToArray(), out var response);

        Assert.IsTrue(result);
        Assert.IsNotNull(response);
        Assert.AreEqual(CMD_ID, response!.Header.MainHeader.CmdId);
        CollectionAssert.AreEqual(tmpResponse.RawBytes.ToArray(), response.RawBytes.ToArray());
        CollectionAssert.AreEqual(payload, response.Payload);
    }

    /// <summary>
    /// Enough data for the packet. Payload absent
    /// </summary>
    [Test]
    public void TryParse_EnoughDataWithoutPayload()
    {
        const int CMD_ID = 3151;
        byte[] payload = null!;
        var tmpResponse = new Response(CMD_ID, payload);

        var result = Response.TryParse(tmpResponse.RawBytes.ToArray(), out var response);

        Assert.IsTrue(result);
        Assert.IsNotNull(response);
        Assert.AreEqual(CMD_ID, response!.Header.MainHeader.CmdId);
        CollectionAssert.AreEqual(tmpResponse.RawBytes.ToArray(), response.RawBytes.ToArray());
        CollectionAssert.AreEqual(payload, response.Payload);
    }

    /// <summary>
    /// Not enough data for the packet
    /// </summary>
    [Test]
    public void TryParse_NotEnoughData()
    {
        var result = Response.TryParse([10, 20, 30], out var response);

        Assert.IsFalse(result);
        Assert.IsNull(response);
    }

    /// <summary>
    /// A packet with cmdId == 0 is recognized as an event and read directly from the response
    /// </summary>
    [Test]
    public void IsEvent_And_TryReadEvent_ForEventPacket()
    {
        var evt = new Event(10, 12, [1, 2, 3]);

        Assert.IsTrue(Response.TryParse(evt.RawBytes.ToArray(), out var response));
        Assert.IsTrue(response!.IsEvent);

        Assert.IsTrue(response.TryReadEvent(out var parsedEvt));
        Assert.AreEqual(evt.Header.ServiceHeader.Service, parsedEvt!.Header.ServiceHeader.Service);
        Assert.AreEqual(evt.Header.ServiceHeader.Command, parsedEvt.Header.ServiceHeader.Command);
        CollectionAssert.AreEqual(evt.Payload, parsedEvt.Payload);
    }

    /// <summary>
    /// A regular response (cmdId != 0) is not an event and is not read as one
    /// </summary>
    [Test]
    public void IsNotEvent_ForRegularResponse()
    {
        var source = new Response(777, [9, 9]);

        Assert.IsTrue(Response.TryParse(source.RawBytes.ToArray(), out var response));
        Assert.IsFalse(response!.IsEvent);

        Assert.IsFalse(response.TryReadEvent(out var evt));
        Assert.IsNull(evt);
    }

    /// <summary>
    /// ToString contains cmdId and payload size
    /// </summary>
    [Test]
    public void ToString_ContainsCmdIdAndPayloadSize()
    {
        var text = new Response(777, [1, 2]).ToString();

        StringAssert.Contains("Response", text);
        StringAssert.Contains("#777", text);
        StringAssert.Contains("payload=2B", text);
        StringAssert.Contains("payload=0B", new Response(777).ToString());
    }
}