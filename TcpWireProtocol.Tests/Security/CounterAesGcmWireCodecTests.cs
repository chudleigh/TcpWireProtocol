using System;
using System.Buffers.Binary;
using System.IO;
using System.Security.Cryptography;
using NUnit.Framework;
using TcpWireProtocol.Packets;
using TcpWireProtocol.Security;

namespace TcpWireProtocol.Tests.Security;

/// <summary>
/// The counter codec derives the nonce from (direction, per-frame counter) and never
/// puts it on the wire. These tests pin down the security-relevant behaviour: replayed,
/// reordered and reflected frames are rejected, and both sides stay in lockstep over a
/// stream of frames.
/// </summary>
public class CounterAesGcmWireCodecTests
{
    /// <summary>
    /// Client-to-server round-trip: the peer with the same key decrypts and reparses
    /// </summary>
    [Test]
    public void RoundTrip_ClientToServer()
    {
        var (client, server) = Pair();
        using (client)
        using (server)
        {
            var request = new Request(10, 12, [1, 2, 3, 4, 5]);
            var wire = client.Encode(request);

            CollectionAssert.AreNotEqual(request.RawBytes.ToArray(), wire);
            Assert.IsTrue(server.TryDecode(wire, out var raw));
            Assert.IsTrue(Request.TryParse(raw!, out var parsed));
            CollectionAssert.AreEqual(request.Payload, parsed!.Payload);
        }
    }

    /// <summary>
    /// Full duplex: both directions work on the same shared key
    /// </summary>
    [Test]
    public void RoundTrip_BothDirections()
    {
        var (client, server) = Pair();
        using (client)
        using (server)
        {
            var request = new Request(10, 12, [1, 2, 3]);
            Assert.IsTrue(server.TryDecode(client.Encode(request), out var rawRequest));
            Assert.IsTrue(Request.TryParse(rawRequest!, out var parsedRequest));

            var response = new Response(parsedRequest!.Header.MainHeader.CmdId, [9, 9]);
            Assert.IsTrue(client.TryDecode(server.Encode(response), out var rawResponse));
            Assert.IsTrue(Response.TryParse(rawResponse!, out var parsedResponse));
            Assert.AreEqual(parsedRequest.Header.MainHeader.CmdId, parsedResponse!.Header.MainHeader.CmdId);
        }
    }

    /// <summary>
    /// The nonce is not transmitted: the frame is exactly length + tag + body
    /// </summary>
    [Test]
    public void Frame_HasNoNonceOnWire()
    {
        var (client, server) = Pair();
        using (client)
        using (server)
        {
            var request = new Request(10, 12, [1, 2, 3, 4, 5, 6, 7]);
            var wire = client.Encode(request);

            Assert.AreEqual(CounterAesGcmWireCodec.FRAME_OVERHEAD + request.RawBytes.Length, wire.Length);
            // The first 4 bytes of the frame are the open body length (LE)
            Assert.AreEqual(request.RawBytes.Length, BinaryPrimitives.ReadInt32LittleEndian(wire));
        }
    }

    /// <summary>
    /// Replay: a frame that already decoded once is rejected the second time
    /// </summary>
    [Test]
    public void Replay_ReturnsCorrupt()
    {
        var (client, server) = Pair();
        using (client)
        using (server)
        {
            var wire = client.Encode(new Request(10, 12, [1, 2, 3, 4]));

            Assert.AreEqual(WireDecodeStatus.Ok, server.TryDecode(wire, 0, wire.Length, out _, out _));
            Assert.AreEqual(WireDecodeStatus.Corrupt, server.TryDecode(wire, 0, wire.Length, out _, out _));
        }
    }

    /// <summary>
    /// Reordering / dropping: skipping a frame desynchronizes the counter — Corrupt
    /// </summary>
    [Test]
    public void SkippedFrame_ReturnsCorrupt()
    {
        var (client, server) = Pair();
        using (client)
        using (server)
        {
            _ = client.Encode(new Request(1, 1, [1]));               // frame #0 is "lost"
            var second = client.Encode(new Request(2, 2, [2, 2]));   // frame #1 arrives first

            Assert.AreEqual(WireDecodeStatus.Corrupt, server.TryDecode(second, 0, second.Length, out _, out _));
        }
    }

    /// <summary>
    /// Reflection: a client frame bounced back to the client fails despite the shared key
    /// </summary>
    [Test]
    public void ReflectedFrame_ReturnsCorrupt()
    {
        var key = Key();
        using var client = new CounterAesGcmWireCodec(key, WireDirection.ClientToServer);

        var wire = client.Encode(new Request(10, 12, [1, 2, 3, 4]));

        // The client expects server->client nonces; its own frame was sealed as client->server.
        Assert.AreEqual(WireDecodeStatus.Corrupt, client.TryDecode(wire, 0, wire.Length, out _, out _));
    }

    /// <summary>
    /// A failed decode does not advance the receive counter: the genuine frame still decodes
    /// </summary>
    [Test]
    public void FailedDecode_DoesNotAdvanceCounter()
    {
        var (client, server) = Pair();
        using (client)
        using (server)
        {
            var request = new Request(10, 12, [1, 2, 3, 4]);
            var wire = client.Encode(request);

            var tampered = (byte[])wire.Clone();
            tampered[^1] ^= 0x01;
            Assert.AreEqual(WireDecodeStatus.Corrupt, server.TryDecode(tampered, 0, tampered.Length, out _, out _));

            // The untouched original is still frame #0 and decodes fine.
            Assert.AreEqual(WireDecodeStatus.Ok, server.TryDecode(wire, 0, wire.Length, out var raw, out _));
            Assert.IsTrue(Request.TryParse(raw!, out var parsed));
            CollectionAssert.AreEqual(request.Payload, parsed!.Payload);
        }
    }

    /// <summary>
    /// Tampering with the open length is caught by the body tag (length is in the AAD)
    /// </summary>
    [Test]
    public void TamperedLength_ReturnsCorrupt()
    {
        var (client, server) = Pair();
        using (client)
        using (server)
        {
            var wire = client.Encode(new Request(10, 12, [1, 2, 3, 4]));

            var bodyLen = wire.Length - CounterAesGcmWireCodec.FRAME_OVERHEAD;
            BinaryPrimitives.WriteInt32LittleEndian(wire.AsSpan(0, CounterAesGcmWireCodec.LENGTH_PREFIX_SIZE), bodyLen - 1);

            Assert.AreEqual(WireDecodeStatus.Corrupt, server.TryDecode(wire, 0, wire.Length, out _, out _));
        }
    }

    /// <summary>
    /// A different key does not decrypt
    /// </summary>
    [Test]
    public void WrongKey_ReturnsCorrupt()
    {
        using var enc = new CounterAesGcmWireCodec(Key(), WireDirection.ClientToServer);
        using var dec = new CounterAesGcmWireCodec(Key(), WireDirection.ServerToClient);

        var wire = enc.Encode(new Request(10, 12, [1, 2, 3, 4]));

        Assert.AreEqual(WireDecodeStatus.Corrupt, dec.TryDecode(wire, 0, wire.Length, out _, out _));
    }

    /// <summary>
    /// External framing: a stream of frames is split by offset + consumed, counters in lockstep
    /// </summary>
    [Test]
    public void ExternalFraming_MultipleMessages_InOrder()
    {
        var (client, server) = Pair();
        using (client)
        using (server)
        {
            using var ms = new MemoryStream();
            ms.Write(client.Encode(new Request(1, 1, [1])));
            ms.Write(client.Encode(new Request(2, 2, [2, 2])));
            ms.Write(client.Encode(new Request(3, 3, [3, 3, 3])));
            var buffer = ms.ToArray();

            var offset = 0;
            var frames = 0;
            while (offset < buffer.Length)
            {
                var status = server.TryDecode(buffer, offset, buffer.Length - offset, out var pt, out var consumed);
                Assert.AreEqual(WireDecodeStatus.Ok, status);
                Assert.IsTrue(Request.TryParse(pt!, out _));
                offset += consumed;
                frames++;
            }

            Assert.AreEqual(3, frames);
            Assert.AreEqual(buffer.Length, offset);
        }
    }

    /// <summary>
    /// Trailing partial frame: NeedMoreData, nothing consumed, and the retry succeeds
    /// </summary>
    [Test]
    public void ExternalFraming_TrailingPartial_RetrySucceeds()
    {
        var (client, server) = Pair();
        using (client)
        using (server)
        {
            var first = client.Encode(new Request(1, 1, [1, 2]));
            var second = client.Encode(new Request(2, 2, [3, 4]));

            using var ms = new MemoryStream();
            ms.Write(first);
            ms.Write(second, 0, second.Length - 5); // the second one is truncated
            var buffer = ms.ToArray();

            Assert.AreEqual(WireDecodeStatus.Ok, server.TryDecode(buffer, 0, buffer.Length, out _, out var consumed));
            Assert.AreEqual(first.Length, consumed);

            Assert.AreEqual(WireDecodeStatus.NeedMoreData,
                server.TryDecode(buffer, consumed, buffer.Length - consumed, out _, out _));

            // The "socket" delivers the rest — the same frame now decodes as frame #1.
            using var full = new MemoryStream();
            full.Write(first);
            full.Write(second);
            var completed = full.ToArray();

            Assert.AreEqual(WireDecodeStatus.Ok,
                server.TryDecode(completed, consumed, completed.Length - consumed, out var raw, out _));
            Assert.IsTrue(Request.TryParse(raw!, out var parsed));
            CollectionAssert.AreEqual(new byte[] { 3, 4 }, parsed!.Payload);
        }
    }

    /// <summary>
    /// No key means the well-known zero key: two independent sides interoperate by default
    /// </summary>
    [Test]
    public void Factory_DefaultZeroKey_Interoperates()
    {
        using var client = (CounterAesGcmWireCodec)WireCodec.Create(WireDirection.ClientToServer);
        using var server = (CounterAesGcmWireCodec)WireCodec.Create(WireDirection.ServerToClient);

        var request = new Request(10, 12, [1, 2, 3]);
        Assert.IsTrue(server.TryDecode(client.Encode(request), out var raw));
        CollectionAssert.AreEqual(request.RawBytes.ToArray(), raw);
    }

    /// <summary>
    /// Factory: an explicit key round-trips the same as a directly constructed codec
    /// </summary>
    [Test]
    public void Factory_ExplicitKey_RoundTrips()
    {
        var key = Key();
        using var client = (CounterAesGcmWireCodec)WireCodec.Create(WireDirection.ClientToServer, key);
        using var server = new CounterAesGcmWireCodec(key, WireDirection.ServerToClient);

        var request = new Request(10, 12, [1, 2, 3]);
        Assert.IsTrue(server.TryDecode(client.Encode(request), out var raw));
        CollectionAssert.AreEqual(request.RawBytes.ToArray(), raw);
    }

    /// <summary>
    /// ZeroKey returns a fresh all-zero 32-byte copy each time
    /// </summary>
    [Test]
    public void ZeroKey_FreshAllZeroCopy()
    {
        var a = WireCodec.ZeroKey;
        var b = WireCodec.ZeroKey;

        Assert.AreEqual(32, a.Length);
        CollectionAssert.AreEqual(new byte[32], a);
        Assert.AreNotSame(a, b); // mutating one copy must not poison the default
    }

    /// <summary>
    /// An empty payload is encrypted and restored
    /// </summary>
    [Test]
    public void RoundTrip_NoPayload()
    {
        var (client, server) = Pair();
        using (client)
        using (server)
        {
            var request = new Request(10, 12);
            Assert.IsTrue(server.TryDecode(client.Encode(request), out var raw));
            CollectionAssert.AreEqual(request.RawBytes.ToArray(), raw);
        }
    }

    /// <summary>
    /// Round-trip for response and event packets
    /// </summary>
    [Test]
    public void RoundTrip_ResponseAndEvent()
    {
        var (client, server) = Pair();
        using (client)
        using (server)
        {
            var response = new Response(777, [5, 6, 7]);
            Assert.IsTrue(client.TryDecode(server.Encode(response), out var responseRaw));
            Assert.IsTrue(Response.TryParse(responseRaw!, out var parsedResponse));
            Assert.AreEqual(777, parsedResponse!.Header.MainHeader.CmdId);

            var evt = new Event(10, 12, [9, 8, 7, 6]);
            Assert.IsTrue(client.TryDecode(server.Encode(evt), out var evtRaw));
            Assert.IsTrue(Event.TryParse(evtRaw!, out var parsedEvt));
            CollectionAssert.AreEqual(evt.Payload, parsedEvt!.Payload);
        }
    }

    /// <summary>
    /// Too short a buffer — NeedMoreData, nothing consumed, no exception
    /// </summary>
    [Test]
    public void TooShort_ReturnsNeedMoreData()
    {
        var (client, server) = Pair();
        using (client)
        using (server)
        {
            Assert.AreEqual(WireDecodeStatus.NeedMoreData, server.TryDecode([1, 2, 3], 0, 3, out var pt, out var consumed));
            Assert.AreEqual(0, consumed);
            Assert.IsNull(pt);
        }
    }

    /// <summary>
    /// A negative length in the prefix — Corrupt
    /// </summary>
    [Test]
    public void NegativeLength_ReturnsCorrupt()
    {
        var (client, server) = Pair();
        using (client)
        using (server)
        {
            var wire = client.Encode(new Request(10, 12, [1, 2, 3, 4]));
            BinaryPrimitives.WriteInt32LittleEndian(wire.AsSpan(0, CounterAesGcmWireCodec.LENGTH_PREFIX_SIZE), -1);

            Assert.AreEqual(WireDecodeStatus.Corrupt, server.TryDecode(wire, 0, wire.Length, out _, out _));
        }
    }

    /// <summary>
    /// TryDecode with invalid offset/count — exception
    /// </summary>
    [Test]
    public void TryDecode_BadRange_Throws()
    {
        var (client, server) = Pair();
        using (client)
        using (server)
        {
            var wire = client.Encode(new Request(10, 12, [1, 2, 3]));

            Assert.Throws<ArgumentOutOfRangeException>(() => server.TryDecode(wire, -1, wire.Length, out _, out _));
            Assert.Throws<ArgumentOutOfRangeException>(() => server.TryDecode(wire, 0, wire.Length + 1, out _, out _));
            Assert.Throws<ArgumentNullException>(() => server.TryDecode((byte[])null!, out _));
        }
    }

    /// <summary>
    /// The only accepted key size is 32 bytes (AES-256); everything else is rejected
    /// </summary>
    [TestCase(0)]    // empty
    [TestCase(16)]   // AES-128 is not supported by this protocol
    [TestCase(24)]   // AES-192 is not supported by this protocol
    [TestCase(31)]   // one byte short
    [TestCase(33)]   // one byte over
    [TestCase(64)]   // too long
    public void NonThirtyTwoByteKey_Throws(int keyLength)
    {
        var ex = Assert.Throws<ArgumentException>(() => new CounterAesGcmWireCodec(new byte[keyLength], WireDirection.ClientToServer));
        Assert.AreEqual("key", ex!.ParamName);
    }

    /// <summary>
    /// Bad constructor arguments are rejected with clear Argument* exceptions
    /// </summary>
    [Test]
    public void Constructor_BadArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => new CounterAesGcmWireCodec(null!, WireDirection.ClientToServer));
        Assert.Throws<ArgumentException>(() => new CounterAesGcmWireCodec(new byte[10], WireDirection.ClientToServer));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CounterAesGcmWireCodec(Key(), (WireDirection)3));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CounterAesGcmWireCodec(Key(), WireDirection.ClientToServer, 0));
    }

    /// <summary>
    /// Body longer than maxMessageLength — Corrupt before any allocation
    /// </summary>
    [Test]
    public void BodyOverMaxLength_ReturnsCorrupt()
    {
        var key = Key();
        using var enc = new CounterAesGcmWireCodec(key, WireDirection.ClientToServer);
        using var dec = new CounterAesGcmWireCodec(key, WireDirection.ServerToClient, maxMessageLength: 4);

        var wire = enc.Encode(new Request(10, 12, [1, 2, 3, 4, 5]));

        Assert.AreEqual(WireDecodeStatus.Corrupt, dec.TryDecode(wire, 0, wire.Length, out _, out _));
    }

    private static byte[] Key()
    {
        return RandomNumberGenerator.GetBytes(32);
    }

    /// <summary>Creates a connected client/server codec pair sharing one key.</summary>
    private static (CounterAesGcmWireCodec client, CounterAesGcmWireCodec server) Pair(byte[]? key = null)
    {
        key ??= Key();
        return (
            new CounterAesGcmWireCodec(key, WireDirection.ClientToServer),
            new CounterAesGcmWireCodec(key, WireDirection.ServerToClient));
    }
}