using System;
using System.Text;
using System.Threading.Tasks;
using TcpWireProtocol.Packets;
using TcpWireProtocol.Samples.Common;
using TcpWireProtocol.Samples.DiffieHellman.Shared;
using TcpWireProtocol.Security;

namespace TcpWireProtocol.Samples.DiffieHellman.Client;

/// <summary>
/// Diffie-Hellman client: opens an unencrypted (zero-key) connection, agrees a shared key over the
/// wire protocol itself, then an interactive loop — type a message to echo it over the encrypted
/// channel, or "rekey" to agree a fresh key.
/// </summary>
internal static class Program
{
    /// <summary>Connects, agrees the first key, then echoes typed messages or rekeys on "rekey".</summary>
    private static async Task Main()
    {
        var port = int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var p) ? p : 5000;

        using var tcp = await WireClient.ConnectWithRetryAsync("127.0.0.1", port);
        using var conn = FramedConnection.Client(tcp.GetStream());   // open (zero key) until the handshake

        var key = await ExchangeKeyAsync(conn);
        Console.WriteLine($"handshake: key {SecureHandshake.Fingerprint(key)}");
        Console.WriteLine("type a message to echo, or 'rekey' to change the key (Ctrl+C to quit)");

        string? line;
        while ((line = Console.ReadLine()) is not null)
        {
            if (line == "rekey")
            {
                var newKey = await ExchangeKeyAsync(conn);
                Console.WriteLine($"rekeyed: key {SecureHandshake.Fingerprint(newKey)}");
            }
            else
            {
                await EchoAsync(conn, line);
            }
        }
    }

    /// <summary>Sends text and prints the echoed reply.</summary>
    private static async Task EchoAsync(FramedConnection conn, string text)
    {
        await conn.SendAsync(new Request(Protocol.APP, Protocol.ECHO, Encoding.UTF8.GetBytes(text)));

        var raw = await conn.ReceiveAsync();
        if (raw is not null && Response.TryParse(raw, out var response))
        {
            Console.WriteLine($"echo -> {Encoding.UTF8.GetString(response.Payload ?? [])}");
        }
    }

    /// <summary>
    /// Agrees a shared key over the current channel (open for the handshake, encrypted for rekeys)
    /// and switches to it. A barrier: run it with no other request in flight (the sequential flow
    /// guarantees that), so no frame crosses the key switch under the wrong codec.
    /// </summary>
    private static async Task<byte[]> ExchangeKeyAsync(FramedConnection conn)
    {
        using var ours = SecureHandshake.CreateKeyPair();
        await conn.SendAsync(new Request(Protocol.APP, Protocol.KEY_EXCHANGE, SecureHandshake.ExportPublic(ours)));

        var raw = await conn.ReceiveAsync();
        if (raw is null || !Response.TryParse(raw, out var response))
        {
            throw new InvalidOperationException("key exchange failed: no reply from server");
        }

        var key = SecureHandshake.DeriveKey(ours, response.Payload!);
        conn.Rekey(WireCodec.Create(WireDirection.ClientToServer, key));
        return key;
    }
}