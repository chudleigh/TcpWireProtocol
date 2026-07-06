using System;
using System.Threading.Tasks;
using TcpWireProtocol.Packets;
using TcpWireProtocol.Samples.Common;
using TcpWireProtocol.Samples.Protobuf.Contracts;

namespace TcpWireProtocol.Samples.Protobuf.Client;

/// <summary>
/// Protobuf client: on every Enter, sends a random <see cref="Person"/> (occasionally null)
/// and prints the server's <see cref="Ack"/>.
/// </summary>
internal static class Program
{
    /// <summary>Connects, then sends a random person (sometimes null) on each Enter until end of input.</summary>
    private static async Task Main()
    {
        var port = int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var p) ? p : 5000;

        using var tcp = await WireClient.ConnectWithRetryAsync("127.0.0.1", port);
        using var conn = FramedConnection.Client(tcp.GetStream());

        Console.WriteLine("press Enter to send a random person (Ctrl+C to quit)");
        while (Console.ReadLine() is not null)
        {
            // Roughly one in four is a null, to exercise the server's rejection path.
            var person = Random.Shared.Next(4) == 0 ? null : NewRandomPerson();
            await ExchangeAsync(conn, person);
        }
    }

    /// <summary>Sends one person (or null) and prints the server's Ack.</summary>
    private static async Task ExchangeAsync(FramedConnection conn, Person? person)
    {
        Console.WriteLine(person is null
            ? "-> (null)"
            : $"-> {person.FirstName} {person.LastName}, born {person.BirthDate:yyyy-MM-dd}, {person.Emails.Count} email(s)");

        await conn.SendAsync(new Request(service: 0, command: 0, ProtoCodec.Serialize(person)));

        var raw = await conn.ReceiveAsync();
        if (raw is null || !Response.TryParse(raw, out var response)) { return; }

        var ack = ProtoCodec.Deserialize<Ack>(response.Payload);
        Console.WriteLine(ack is null ? "server: (null)" : $"server: ok={ack.Ok}, {ack.Message}");
    }

    /// <summary>Builds a person with a random name, birth date, and email.</summary>
    private static Person NewRandomPerson()
    {
        var first = _firstNames[Random.Shared.Next(_firstNames.Length)];
        var last = _lastNames[Random.Shared.Next(_lastNames.Length)];
        var domain = _domains[Random.Shared.Next(_domains.Length)];
        var birth = new DateTime(Random.Shared.Next(1990, 2020), Random.Shared.Next(1, 13), Random.Shared.Next(1, 29));

        return new Person
        {
            FirstName = first,
            LastName = last,
            BirthDate = birth,
            Emails = [$"{first.ToLowerInvariant()}.{last.ToLowerInvariant()}@{domain}"],
        };
    }

    private static readonly string[] _firstNames = ["Ada", "Alan", "Grace", "Linus", "Margaret", "Dennis", "Barbara", "Ken"];
    private static readonly string[] _lastNames = ["Lovelace", "Turing", "Hopper", "Torvalds", "Hamilton", "Ritchie", "Liskov", "Thompson"];
    private static readonly string[] _domains = ["example.com", "mail.test", "dev.local"];
}