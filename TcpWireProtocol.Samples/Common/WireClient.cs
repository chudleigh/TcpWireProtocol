using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace TcpWireProtocol.Samples.Common;

/// <summary>Connect helpers for the client side of a sample.</summary>
public static class WireClient
{
    /// <summary>Opens a TCP connection; wrap the result with <see cref="FramedConnection.Client"/>.</summary>
    public static async Task<TcpClient> ConnectAsync(string host, int port, CancellationToken ct = default)
    {
        var client = new TcpClient();
        await client.ConnectAsync(host, port, ct);
        return client;
    }

    /// <summary>Opens a TCP connection, retrying every second until the server accepts it (Ctrl+C to give up).</summary>
    public static async Task<TcpClient> ConnectWithRetryAsync(string host, int port, CancellationToken ct = default)
    {
        var announced = false;
        while (true)
        {
            try
            {
                return await ConnectAsync(host, port, ct);
            }
            catch (SocketException)
            {
                if (!announced)
                {
                    Console.WriteLine($"waiting for server on {host}:{port}...");
                    announced = true;
                }

                await Task.Delay(TimeSpan.FromSeconds(1), ct);
            }
        }
    }
}