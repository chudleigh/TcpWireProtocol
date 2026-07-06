using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TcpWireProtocol.Packets;
using TcpWireProtocol.Samples.Common;
using TcpWireProtocol.Samples.Rpc.Contracts;

namespace TcpWireProtocol.Samples.Rpc.Server;

/// <summary>
/// Command server: routes each (service, command) from the header to its own async handler.
/// Handlers run concurrently and take variable time, so replies finish out of request order;
/// each still carries the request's CmdId for the client to match.
/// Services: Calc (add/subtract/multiply) and Text (upper/reverse).
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

    /// <summary>Serves one connection: dispatches each request on its own task and awaits them all.</summary>
    private static async Task HandleClientAsync(NetworkStream stream, CancellationToken ct)
    {
        var peer = stream.Socket.RemoteEndPoint;
        Console.WriteLine($"[{peer}] connected");

        using var conn = FramedConnection.Server(stream);
        using var sendGate = new SemaphoreSlim(1, 1);
        var inFlight = new List<Task>();

        await foreach (var raw in conn.ReceiveAllAsync(ct))
        {
            if (!Request.TryParse(raw, out var request)) { continue; }
            inFlight.Add(ProcessAsync(conn, sendGate, request, peer, ct));
        }

        await Task.WhenAll(inFlight);
        Console.WriteLine($"[{peer}] disconnected");
    }

    /// <summary>Dispatches one request to its async handler, then sends the reply (sends are serialized).</summary>
    private static async Task ProcessAsync(FramedConnection conn, SemaphoreSlim sendGate, Request request, EndPoint? peer, CancellationToken ct)
    {
        var service = request.Header.ServiceHeader.Service;
        var command = request.Header.ServiceHeader.Command;

        byte[]? reply;
        try
        {
            reply = await _router.DispatchAsync(service, command, request.Payload);
        }
        catch (InvalidOperationException ex)
        {
            reply = Encoding.UTF8.GetBytes($"error: {ex.Message}");
        }

        // The codec is single-writer: serialize replies while the receive loop keeps reading.
        await sendGate.WaitAsync(ct);
        try
        {
            await conn.SendAsync(request.CreateResponse(reply), ct);
            Console.WriteLine($"[{peer}] reply #{request.Header.MainHeader.CmdId} (svc {service} cmd {command})");
        }
        finally
        {
            sendGate.Release();
        }
    }

    private static readonly CommandRouter _router = new CommandRouter()
        .On(Services.CALC, CalcCommands.ADD, CalcService.AddAsync)
        .On(Services.CALC, CalcCommands.SUBTRACT, CalcService.SubtractAsync)
        .On(Services.CALC, CalcCommands.MULTIPLY, CalcService.MultiplyAsync)
        .On(Services.TEXT, TextCommands.UPPER, TextService.ToUpperAsync)
        .On(Services.TEXT, TextCommands.REVERSE, TextService.ReverseAsync);
}