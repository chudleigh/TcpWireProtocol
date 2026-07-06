using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using TcpWireProtocol.Packets;
using TcpWireProtocol.Samples.Common;

namespace TcpWireProtocol.Samples.Rpc.Client;

/// <summary>
/// Multiplexes many concurrent commands over one connection. Each request carries a unique
/// <c>CmdId</c>; a background loop reads replies and matches each one back to its caller by that
/// id, so requests can be in flight together and complete in any order.
/// </summary>
internal sealed class CommandChannel
{
    /// <summary>Starts the background receive loop over an already-connected framed connection.</summary>
    public CommandChannel(FramedConnection conn)
    {
        _conn = conn;
        _ = ReceiveLoopAsync();
    }

    /// <summary>Sends one command and awaits its reply, correlated by CmdId.</summary>
    public async Task<byte[]?> CallAsync(short service, short command, byte[]? payload)
    {
        var request = new Request(service, command, payload);
        var reply = new TaskCompletionSource<byte[]?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[request.Header.MainHeader.CmdId] = reply;

        // The codec is single-writer: serialize sends while the receive loop runs concurrently.
        await _sendGate.WaitAsync();
        try
        {
            await _conn.SendAsync(request);
        }
        finally
        {
            _sendGate.Release();
        }

        return await reply.Task;
    }

    /// <summary>Reads replies and completes the matching pending call until the connection closes.</summary>
    private async Task ReceiveLoopAsync()
    {
        try
        {
            byte[]? raw;
            while ((raw = await _conn.ReceiveAsync()) is not null)
            {
                if (Response.TryParse(raw, out var response) &&
                    _pending.TryRemove(response.Header.MainHeader.CmdId, out var reply))
                {
                    reply.TrySetResult(response.Payload);
                }
            }
        }
        catch (Exception ex)
        {
            foreach (var reply in _pending.Values) { reply.TrySetException(ex); }
        }
    }

    private readonly FramedConnection _conn;
    private readonly ConcurrentDictionary<int, TaskCompletionSource<byte[]?>> _pending = new();
    private readonly SemaphoreSlim _sendGate = new(1, 1);
}