using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace TcpWireProtocol.Samples.Common;

/// <summary>Connect helper for the client side of a sample.</summary>
public static class WireClient
{
    /// <summary>Opens a TCP connection; wrap the result with <see cref="FramedConnection.Client"/>.</summary>
    public static async Task<TcpClient> ConnectAsync(string host, int port, CancellationToken ct = default)
    {
        var client = new TcpClient();
        await client.ConnectAsync(host, port, ct);
        return client;
    }
}