using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TcpWireProtocol.Samples.Rpc.Server;

/// <summary>Maps (service, command) pairs to async handlers and dispatches request payloads to them.</summary>
internal sealed class CommandRouter
{
    /// <summary>Registers a handler for one (service, command) pair; returns this for chaining.</summary>
    public CommandRouter On(short service, short command, Func<byte[]?, Task<byte[]?>> handler)
    {
        _handlers[(service, command)] = handler;
        return this;
    }

    /// <summary>Runs the handler for the given (service, command), or throws if none is registered.</summary>
    public Task<byte[]?> DispatchAsync(short service, short command, byte[]? payload)
    {
        if (!_handlers.TryGetValue((service, command), out var handler))
        {
            throw new InvalidOperationException($"no handler for service {service}, command {command}");
        }

        return handler(payload);
    }

    private readonly Dictionary<(short Service, short Command), Func<byte[]?, Task<byte[]?>>> _handlers = [];
}
