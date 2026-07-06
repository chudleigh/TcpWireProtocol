using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TcpWireProtocol.Packets;
using TcpWireProtocol.Samples.Common;

namespace TcpWireProtocol.Samples.Echo.Server;

/// <summary>
/// Echo server: reads Requests off each connection and replies with the same payload.
/// No key: the open "zero key" mode.
/// </summary>
internal static class Program
{
    /// <summary>Starts the echo server on the loopback port (from PORT, default 5000).</summary>
    private static async Task Main()
    {
        var port = int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var p) ? p : 5000;
        var server = new WireServer(new IPEndPoint(IPAddress.Loopback, port), HandleClientAsync);
        await server.RunAsync();
    }

    /// <summary>Serves one connection: echoes every request straight back as a response.</summary>
    private static async Task HandleClientAsync(NetworkStream stream, CancellationToken ct)
    {
        var peer = stream.Socket.RemoteEndPoint;
        Console.WriteLine($"[{peer}] connected");

        using var conn = FramedConnection.Server(stream);

        await foreach (var raw in conn.ReceiveAllAsync(ct))
        {
            if (!Request.TryParse(raw, out var request)) { continue; }

            Console.WriteLine($"[{peer}] <- {Encoding.UTF8.GetString(request.Payload ?? [])}");
            await conn.SendAsync(request.CreateResponse(request.Payload), ct);
        }

        Console.WriteLine($"[{peer}] disconnected");
    }
}