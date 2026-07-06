using System;

using NUnit.Framework;
using TcpWireProtocol.Packets;

namespace TcpWireProtocol.Tests.Packets;

public class EventTests
{
    /// <summary>
    /// Packet data is null
    /// </summary>
    [Test]
    public void TryParse_ArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Event.TryParse(null!, out var evt));
    }

    /// <summary>
    /// Enough data for the packet. Payload present
    /// </summary>
    [Test]
    public void TryParse_EnoughDataWithPayload()
    {
        const short SERVICE = 10;
        const short COMMAND = 12;
        var payload = new byte[] { 10, 20, 30 };
        var tmpEvt = new Event(SERVICE, COMMAND, payload);

        var result = Event.TryParse(tmpEvt.RawBytes.ToArray(), out var evt);

        Assert.IsTrue(result);
        Assert.IsNotNull(evt);
        CollectionAssert.AreEqual(tmpEvt.RawBytes.ToArray(), evt!.RawBytes.ToArray());
        CollectionAssert.AreEqual(tmpEvt.Payload, evt.Payload);
        CollectionAssert.AreEqual(evt.Payload, payload);
    }

    /// <summary>
    /// Enough data for the packet. Payload absent
    /// </summary>
    [Test]
    public void TryParse_EnoughDataWithoutPayload()
    {
        const short SERVICE = 10;
        const short COMMAND = 12;
        byte[] payload = null!;
        var tmpEvt = new Event(SERVICE, COMMAND, payload);

        var result = Event.TryParse(tmpEvt.RawBytes.ToArray(), out var evt);

        Assert.IsTrue(result);
        Assert.IsNotNull(evt);
        CollectionAssert.AreEqual(tmpEvt.RawBytes.ToArray(), evt!.RawBytes.ToArray());
        CollectionAssert.AreEqual(tmpEvt.Payload, evt.Payload);
        CollectionAssert.AreEqual(evt.Payload, payload);
    }

    /// <summary>
    /// Not enough data for the packet
    /// </summary>
    [Test]
    public void TryParse_NotEnoughData()
    {
        var result = Event.TryParse([10, 20, 30], out var evt);

        Assert.IsFalse(result);
        Assert.IsNull(evt);
    }

    /// <summary>
    /// ToString contains service, command and payload size
    /// </summary>
    [Test]
    public void ToString_ContainsServiceCommandAndPayloadSize()
    {
        var text = new Event(10, 12, [1, 2, 3, 4]).ToString();

        StringAssert.Contains("Event", text);
        StringAssert.Contains("svc=10", text);
        StringAssert.Contains("cmd=12", text);
        StringAssert.Contains("payload=4B", text);
        StringAssert.Contains("payload=0B", new Event(10, 12).ToString());
    }
}