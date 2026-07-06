using System;

using NUnit.Framework;
using TcpWireProtocol.Packets;

namespace TcpWireProtocol.Tests.Packets;

public class RequestTests
{
    /// <summary>
    /// Packet data is null
    /// </summary>
    [Test]
    public void TryParse_ArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Request.TryParse(null!, out var request));
    }

    /// <summary>
    /// Enough data for the packet
    /// </summary>
    [Test]
    public void TryParse_EnoughDataWithPayload()
    {
        const short SERVICE = 10;
        const short COMMAND = 12;
        var payload = new byte[] { 10, 20, 30 };
        var tmpRequest = new Request(SERVICE, COMMAND, payload);

        var result = Request.TryParse(tmpRequest.RawBytes.ToArray(), out var request);

        Assert.IsTrue(result);
        Assert.IsNotNull(request);
        CollectionAssert.AreEqual(tmpRequest.RawBytes.ToArray(), request!.RawBytes.ToArray());
        CollectionAssert.AreEqual(tmpRequest.Payload, request.Payload);
    }

    /// <summary>
    /// Enough data for the packet
    /// </summary>
    [Test]
    public void TryParse_EnoughDataWithoutPayload()
    {
        const short SERVICE = 10;
        const short COMMAND = 12;
        byte[] payload = null!;
        var tmpRequest = new Request(SERVICE, COMMAND, payload);

        var result = Request.TryParse(tmpRequest.RawBytes.ToArray(), out var request);

        Assert.IsTrue(result);
        Assert.IsNotNull(request);
        CollectionAssert.AreEqual(tmpRequest.RawBytes.ToArray(), request!.RawBytes.ToArray());
        CollectionAssert.AreEqual(tmpRequest.Payload, request.Payload);
    }

    /// <summary>
    /// Not enough data for the packet
    /// </summary>
    [Test]
    public void TryParse_NotEnoughData()
    {
        var result = Request.TryParse([10, 20, 30], out var request);

        Assert.IsFalse(result);
        Assert.IsNull(request);
    }

    /// <summary>
    /// Creating a reply to a request. Payload present
    /// </summary>
    [Test]
    public void CreateResponse_WithPayload()
    {
        const short SERVICE = 10;
        const short COMMAND = 12;
        var payload = new byte[] { 10, 20, 30 };

        var tmpRequest = new Request(SERVICE, COMMAND);
        var tmpResponse = tmpRequest.CreateResponse(payload);

        Assert.IsNotNull(tmpResponse);
        Assert.AreEqual(tmpRequest.Header.MainHeader.CmdId, tmpResponse.Header.MainHeader.CmdId);
        CollectionAssert.AreEqual(payload, tmpResponse.Payload);
    }

    /// <summary>
    /// Creating a reply to a request. Payload absent
    /// </summary>
    [Test]
    public void CreateResponse_WithoutPayload()
    {
        const short SERVICE = 10;
        const short COMMAND = 12;
        byte[] payload = null!;

        var tmpRequest = new Request(SERVICE, COMMAND);
        var tmpResponse = tmpRequest.CreateResponse(payload);

        Assert.IsNotNull(tmpResponse);
        Assert.AreEqual(tmpRequest.Header.MainHeader.CmdId, tmpResponse.Header.MainHeader.CmdId);
        CollectionAssert.AreEqual(payload, tmpResponse.Payload);
    }

    /// <summary>
    /// ToString contains cmdId, service, command and payload size
    /// </summary>
    [Test]
    public void ToString_ContainsHeaderFieldsAndPayloadSize()
    {
        var text = new Request(10, 12, [1, 2, 3]).ToString();

        StringAssert.Contains("Request", text);
        StringAssert.Contains("svc=10", text);
        StringAssert.Contains("cmd=12", text);
        StringAssert.Contains("payload=3B", text);
        StringAssert.Contains("payload=0B", new Request(10, 12).ToString());
    }
}