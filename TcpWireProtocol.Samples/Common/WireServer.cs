using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace TcpWireProtocol.Samples.Common;

/// <summary>
/// Minimal async TCP accept loop: listens on an endpoint and serves each accepted connection
/// on its own task. The handler gets the raw <see cref="NetworkStream"/> and frames it itself
/// (usually via <see cref="FramedConnection.Server"/>).
/// </summary>
public sealed class WireServer(IPEndPoint endpoint, Func<NetworkStream, CancellationToken, Task> handler)
{
    /// <summary>Accepts connections until <paramref name="ct"/> is cancelled, serving each on its own task.</summary>
    public async Task RunAsync(CancellationToken ct = default)
    {
        var listener = new TcpListener(endpoint);
        listener.Start();
        Console.WriteLine($"listening on {endpoint}");

        try
        {
            while (!ct.IsCancellationRequested)
            {
                var client = await listener.AcceptTcpClientAsync(ct);
                _ = ServeAsync(client, ct);   // one task per connection
            }
        }
        finally
        {
            listener.Stop();
        }
    }

    /// <summary>Serves one accepted connection and disposes it, keeping per-connection errors isolated.</summary>
    private async Task ServeAsync(TcpClient client, CancellationToken ct)
    {
        using (client)
        {
            try
            {
                await handler(client.GetStream(), ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Console.Error.WriteLine($"connection error: {ex.Message}");
            }
        }
    }
}