using System;
using System.Text;
using System.Threading.Tasks;
using TcpWireProtocol.Packets;
using TcpWireProtocol.Samples.Common;

namespace TcpWireProtocol.Samples.Echo.Client;

/// <summary>
/// Echo client. With args: send them as one message and exit. Without: interactive loop (empty line quits).
/// </summary>
internal static class Program
{
    /// <summary>Connects to the echo server and runs the send/receive loop.</summary>
    private static async Task Main(string[] args)
    {
        var port = int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var p) ? p : 5000;

        using var tcp = await WireClient.ConnectWithRetryAsync("127.0.0.1", port);
        using var conn = FramedConnection.Client(tcp.GetStream());

        if (args.Length > 0)
        {
            await ExchangeAsync(conn, string.Join(' ', args));
        }
        else
        {
            Console.WriteLine("type a message (empty line to quit):");
            string? line;
            while (!string.IsNullOrEmpty(line = Console.ReadLine()))
            {
                await ExchangeAsync(conn, line);
            }
        }
    }

    /// <summary>Sends one text message and prints the echoed reply.</summary>
    private static async Task ExchangeAsync(FramedConnection conn, string text)
    {
        await conn.SendAsync(new Request(service: 0, command: 0, Encoding.UTF8.GetBytes(text)));

        var raw = await conn.ReceiveAsync();
        if (raw is null) { return; }

        if (Response.TryParse(raw, out var response))
        {
            Console.WriteLine($"<- {Encoding.UTF8.GetString(response.Payload ?? [])}");
        }
    }
}