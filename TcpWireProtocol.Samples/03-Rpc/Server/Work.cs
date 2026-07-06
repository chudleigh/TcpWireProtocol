using System;
using System.Threading.Tasks;

namespace TcpWireProtocol.Samples.Rpc.Server;

/// <summary>Simulates a handler doing variable-duration async work.</summary>
internal static class Work
{
    /// <summary>Waits a random short spell to mimic real handler latency.</summary>
    public static Task SimulateAsync()
    {
        return Task.Delay(Random.Shared.Next(50, 400));
    }
}