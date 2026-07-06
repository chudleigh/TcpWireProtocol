using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TcpWireProtocol.Packets;
using TcpWireProtocol.Samples.Common;
using TcpWireProtocol.Samples.DiffieHellman.Shared;
using TcpWireProtocol.Security;

namespace TcpWireProtocol.Samples.DiffieHellman.Server;

/// <summary>
/// Diffie-Hellman server: accepts an open (zero-key) connection, agrees a shared key with the
/// client over the wire protocol itself, then echoes messages over the encrypted channel. Each
/// KEY_EXCHANGE request (the first is the handshake, later ones are rekeys) agrees a fresh key.
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

    /// <summary>Serves one connection: agrees keys on demand and echoes everything else.</summary>
    private static async Task HandleClientAsync(NetworkStream stream, CancellationToken ct)
    {
        var peer = stream.Socket.RemoteEndPoint;
        Console.WriteLine($"[{peer}] connected");

        // Start open (zero key); the first KEY_EXCHANGE agrees the real key.
        using var conn = FramedConnection.Server(stream);

        byte[]? raw;
        while ((raw = await conn.ReceiveAsync(ct)) is not null)
        {
            if (!Request.TryParse(raw, out var request)) { continue; }

            if (request.Header.ServiceHeader.Command == Protocol.KEY_EXCHANGE)
            {
                var key = await ExchangeKeyAsync(conn, request);
                Console.WriteLine($"[{peer}] key {SecureHandshake.Fingerprint(key)}");
                continue;
            }

            Console.WriteLine($"[{peer}] echo: {Encoding.UTF8.GetString(request.Payload ?? [])}");
            await conn.SendAsync(request.CreateResponse(request.Payload), ct);
        }

        Console.WriteLine($"[{peer}] disconnected");
    }

    /// <summary>
    /// Answers a KEY_EXCHANGE request with our fresh public key and switches to the agreed key.
    /// A barrier: valid only with no other request in flight (the sequential loop guarantees that).
    /// </summary>
    private static async Task<byte[]> ExchangeKeyAsync(FramedConnection conn, Request request)
    {
        using var ours = SecureHandshake.CreateKeyPair();
        await conn.SendAsync(request.CreateResponse(SecureHandshake.ExportPublic(ours)));

        var key = SecureHandshake.DeriveKey(ours, request.Payload!);
        conn.Rekey(WireCodec.Create(WireDirection.ServerToClient, key));
        return key;
    }
}