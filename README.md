# TcpWireProtocol

**English** | [Русский](README.ru.md)

A small C# library that takes one annoying job off your hands when working with TCP:
sending and receiving whole messages instead of a stream of bytes. It also encrypts them.

## Why you'd want it

TCP gives you a stream of bytes, not messages. You send one 200-byte packet, and on the
other end `Receive` hands you back 150 bytes first and the remaining 50 later. Or the other
way around: it glues two of your packets into a single buffer. There are no message
boundaries in TCP; you have to mark them yourself.

The usual way is to write each message's length in front of it, and on the receiving side
read that length first and wait until enough bytes have arrived from the socket. It's dull,
mandatory code that's easy to get wrong: under-read, glue two together, slip on a frame
boundary, and the whole stream falls apart. TcpWireProtocol handles that chore for you, and
on top of it encrypts every message with AES-GCM, so what goes over the wire isn't just
framed but protected too.

What the library does **not** do is open sockets or touch the network at all. The transport
stays yours: `Socket`, `NetworkStream`, a channel, a test double. The library sits as a thin
layer between that transport and your logic: on the way out it turns a message object into a
ready-to-send frame, on the way in it reassembles the frame and decrypts it.

## Three kinds of messages

The model is client and server. There are three kinds of messages:

- **`Request`** - a request from client to server. It carries a number (`CmdId`) you can use
  later to match the reply to its request.
- **`Response`** - the server's reply to a specific request. It carries the same `CmdId`.
- **`Event`** - a message the server sends on its own, without a request (for example, "a new
  user just joined").

Where a message goes is decided by two numbers: `service` picks the subsystem (say, auth),
and `command` picks the specific operation inside it (log in, log out, register).

## Getting started

The flow is always the same. To send: build a message, ask the codec to pack it into a
frame, send that frame over your transport.

```csharp
using TcpWireProtocol.Packets;
using TcpWireProtocol.Security;

// Create one codec per connection side. It counts frames internally, so one instance =
// one connection; you can't reuse it across connections.
// key == null means "no real protection" (see the encryption section below).
IWireCodec codec = WireCodec.Create(WireDirection.ClientToServer, key);   // on the client
// on the server:   WireCodec.Create(WireDirection.ServerToClient, key)

var request = new Request(service: 1, command: 1, payload);
transport.Send(codec.Encode(request));      // one encrypted frame with the length up front
```

Receiving is exactly as messy as TCP itself: all you have on hand is whatever has already
arrived from the socket, and a whole frame might not be there yet. So receiving is a loop.
You hand the codec the buffer you've accumulated, and it answers one of three things:

- **`NeedMoreData`** - the frame hasn't fully arrived; read more from the socket and try again;
- **`Ok`** - the frame is decoded; here are the decrypted bytes, and here's how many bytes of
  the buffer it took;
- **`Corrupt`** - forged, replayed, or wrong key; this connection should be closed.

```csharp
// buffer[offset..offset+count] is what you've read from the transport so far.
while (true)
{
    var status = codec.TryDecode(buffer, offset, count, out byte[]? raw, out int consumed);

    if (status == WireDecodeStatus.NeedMoreData)
        break;                          // wait for more bytes from the socket

    if (status == WireDecodeStatus.Corrupt)
    {
        transport.Close();              // the stream can't be resynced, tear the connection down
        break;
    }

    // Ok: raw is the decrypted message, the frame took `consumed` bytes.
    offset += consumed;
    count  -= consumed;

    Handle(raw);
}
```

From there the decrypted bytes turn back into a typed message with `TryParse`. On the server,
incoming data is read as a `Request`, and the reply is convenient to build straight from it:
`CreateResponse` fills in the right `CmdId`:

```csharp
if (Request.TryParse(raw, out Request request))
{
    var response = request.CreateResponse(replyPayload);
    transport.Send(codec.Encode(response));
}
```

On the client, everything incoming is read as a `Response`. One subtlety: events (`Event`)
arrive on the same channel as replies, and they're deliberately built to read as an ordinary
`Response`. You tell them apart by the reserved `CmdId == 0` - that's what `IsEvent` and
`TryReadEvent` are for, the latter re-reading the same buffer as an event (no copy):

```csharp
if (Response.TryParse(raw, out Response response))
{
    if (response.TryReadEvent(out Event evt))
        HandleEvent(evt);           // the server pushed an event on its own
    else
        HandleResponse(response);   // it's a reply to our request, match it by CmdId
}
```

So there's no separate "message type" byte in the protocol, and none is needed: the
connection direction already separates a request from a reply, and `CmdId == 0` separates an
event from a reply.

If you already have a whole frame sitting in a buffer (in a test, say), you don't need the
loop. There's a short overload for that: it takes a complete frame and returns `true`/`false`
instead of the three states.

```csharp
if (codec.TryDecode(frame, out byte[]? raw)) { /* raw is the decrypted message */ }
```

## Examples

Runnable client/server samples live in [`TcpWireProtocol.Samples/`](TcpWireProtocol.Samples/).
There are four, each about a different part of using the library. Each is a pair of console apps
sharing a small `Common` project (the socket glue), so run the server in one terminal and the
client in another:

```
dotnet run --project TcpWireProtocol.Samples/01-Echo/Server
dotnet run --project TcpWireProtocol.Samples/01-Echo/Client
```

**01-Echo** is the bare minimum. The client sends a `Request`, the server replies with a `Response`
carrying the same payload, in the open ("zero key") mode. It shows how `FramedConnection` bridges a
raw `NetworkStream` and the codec: read bytes off the socket, decode whole frames, send packets back.

**02-Protobuf** adds structured payloads. The client serializes a `Person` object with protobuf-net
into the request body; the server deserializes it, runs some stand-in business logic (an age check,
null handling) and replies with an `Ack` object. The payload rides as protobuf's compact binary
format, not text or JSON.

**03-Rpc** is command dispatch with correlation. Two services (`Calc` and `Text`), each with a few
commands, are routed by the header's `(service, command)` to their own async handlers. The client
fires many requests at once over a single connection; the server handles them out of order (each
handler takes a different time), and the client matches every reply back to its call by `CmdId`.

**04-DiffieHellman** is key agreement and live rekeying. On connect, both sides exchange public keys
(ECDH) as ordinary protocol packets sent in the open mode, derive a shared AES-256 key, then talk
encrypted. Typing `rekey` runs a fresh exchange over the encrypted channel and switches both sides to
a new key without dropping the connection.

## Encryption

Encryption lives in the codec and doesn't touch the messages themselves: `Request`,
`Response`, `Event` and their parsing don't know it exists. Under the hood it's AES-GCM: in a
single pass it both encrypts and checks integrity, and it's hardware-accelerated on top. The
key is 32 bytes (AES-256), with no other options, so there's nothing to configure. If a frame
was forged, replayed, or arrived under the wrong key, decryption fails and you get `Corrupt`.
The protocol won't quietly swallow tampered data.

There's no "unencrypted" mode either, and that's on purpose, so the frame format and the code
path stay the same in every case. If you don't pass a key, the codec uses the well-known
all-zero key (`WireCodec.ZeroKey`): technically everything is encrypted, but that key is known
to anyone who has seen the sources, so there's no protection in this mode, it's just an "open
protocol". Real protection comes from a real key: pre-shared, or produced by your own code
from a key exchange (Diffie-Hellman, for instance).

The interesting part is how the nonce works (the one-time value AES-GCM needs). Usually it's
shipped over the wire with every message. Here it's never sent at all: both sides compute it
themselves from the direction and a frame counter that starts at zero on each connection. This
works precisely because TCP is underneath: it delivers frames in order and without loss, so
the sender's counter and the receiver's expected counter move in lockstep. And there's a nice
side effect: if someone replays, reorders, or injects an old frame, it lands on the "wrong"
counter at the receiver, the nonce doesn't match, and the check fails. So replay protection
comes for free, on its own.

One detail worth being upfront about: the message length travels over the wire in the clear
(it can't be encrypted, since it's what marks the frame boundaries). It can't be tampered with
unnoticed, as its value is folded into the body's integrity check. But a passive observer can
see where one message ends and the next begins, and can estimate their sizes. That's the price
of speed: one encryption pass instead of two. If sizes must be hidden, pad messages to a fixed
length or use a two-pass scheme.

A few rules that matter:

- one codec per connection side; it's stateful and not thread-safe;
- the counters live in memory only and reset on a new connection, so if the key is per-session,
  rekey every session;
- got a `Corrupt`? close the connection; the stream can't be reassembled after that.

### Size limit

The codec has an optional `maxMessageLength` parameter (1 MB by default). If an incoming
frame's length exceeds the limit, the codec returns `Corrupt` before allocating a single byte
of memory. That's a guard against someone sending a garbage, huge length to blow up your
memory. The `WireCodec.Create` factory uses the default; if you need a different one, build the
codec directly:

```csharp
var codec = new CounterAesGcmWireCodec(key, WireDirection.ClientToServer, maxMessageLength: 64 * 1024);
```

## Install

```
dotnet add package TcpWireProtocol
```

Built for `netstandard2.1`, `net8.0` and `net10.0`. The `netstandard2.1` build covers
.NET Core 3.0+, .NET 5+, Mono 6.4+, Xamarin and Unity 2021.2+. Classic .NET Framework isn't
supported, as it has no `AesGcm`. No third-party dependencies on any platform.

## Wire format

If you need to implement a compatible peer in another language, here's the layout. All
multi-byte fields are little-endian. Below is the *message* payload (what `TryParse` reads);
on the wire the codec wraps it in a `length(4) | tag(16) | encrypted body` frame, and the
nonce, as noted above, isn't sent but derived from the direction and the counter.

**`Request`**

| cmd id | length | service | command | payload |
| --- | --- | --- | --- | --- |
| int (4) | int (4) | short (2) | short (2) | byte[] (*length*) |

**`Response`**

| cmd id | length | payload |
| --- | --- | --- |
| int (4) | int (4) | byte[] (*length*) |

**`Event`**

| cmd id | length | service | command | payload |
| --- | --- | --- | --- | --- |
| int (4) | int (4) | short (2) | short (2) | byte[] (*length*) |

For an `Event`, the `cmd id` field is always `0`, and `length` includes the size of `service`
and `command`. That's exactly why an event reads as an ordinary `Response`: the leading
`cmd id` and `length` fields line up, while `service` and `command` sit inside what a
`Response` treats as the start of its body.

## License

MIT © chudleigh
