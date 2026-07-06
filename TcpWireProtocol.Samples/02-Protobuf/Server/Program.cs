using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using TcpWireProtocol.Packets;
using TcpWireProtocol.Samples.Common;
using TcpWireProtocol.Samples.Protobuf.Contracts;

namespace TcpWireProtocol.Samples.Protobuf.Server;

/// <summary>
/// Protobuf server: receives a <see cref="Person"/> and replies with an <see cref="Ack"/>.
/// The payloads are protobuf-serialized objects; framing and encryption are unchanged.
/// A null Person, or one under the minimum age, is rejected with an Ack whose <c>Ok</c> is false.
/// </summary>
internal static class Program
{
    /// <summary>Starts the server on the loopback port (from PORT, default 5000).</summary>
    private static async Task Main()
    {
        var port = int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var p) ? p : 5000;
        var server = new WireServer(new IPEndPoint(IPAddress.Loopback, port), HandleClientAsync);
        await server.RunAsync();
    }

    /// <summary>Serves one connection: validates each Person's age and replies with an Ack.</summary>
    private static async Task HandleClientAsync(NetworkStream stream, CancellationToken ct)
    {
        var peer = stream.Socket.RemoteEndPoint;
        Console.WriteLine($"[{peer}] connected");

        using var conn = FramedConnection.Server(stream);

        await foreach (var raw in conn.ReceiveAllAsync(ct))
        {
            if (!Request.TryParse(raw, out var request)) { continue; }

            var person = ProtoCodec.Deserialize<Person>(request.Payload);

            Ack ack;
            if (person is null)
            {
                Console.WriteLine($"[{peer}] <- (null)");
                ack = new Ack { Ok = false, Message = "no person provided" };
            }
            else
            {
                var age = AgeOn(DateTime.Today, person.BirthDate);
                ack = age < MINIMUM_AGE
                    ? new Ack { Ok = false, Message = $"{person.FirstName} {person.LastName} is under {MINIMUM_AGE} (age {age})" }
                    : new Ack { Ok = true, Message = $"registered {person.FirstName} {person.LastName}" };
                Console.WriteLine($"[{peer}] <- {person.FirstName} {person.LastName}, age {age}{(ack.Ok ? "" : " (rejected)")}");
            }

            await conn.SendAsync(request.CreateResponse(ProtoCodec.Serialize(ack)), ct);
        }

        Console.WriteLine($"[{peer}] disconnected");
    }

    /// <summary>Computes a person's age in whole years as of <paramref name="today"/>.</summary>
    private static int AgeOn(DateTime today, DateTime birthDate)
    {
        var age = today.Year - birthDate.Year;
        if (birthDate.Date > today.AddYears(-age)) { age--; }
        return age;
    }

    private const int MINIMUM_AGE = 18;
}