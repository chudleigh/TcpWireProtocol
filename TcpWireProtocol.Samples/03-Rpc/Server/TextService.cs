using System;
using System.Threading.Tasks;
using TcpWireProtocol.Samples.Rpc.Contracts;

namespace TcpWireProtocol.Samples.Rpc.Server;

/// <summary>Text command handlers. Request and reply are both <see cref="TextMessage"/>.</summary>
internal static class TextService
{
    /// <summary>Uppercases the text.</summary>
    public static async Task<byte[]?> ToUpperAsync(byte[]? payload)
    {
        var request = ProtoCodec.Deserialize<TextMessage>(payload);
        await Work.SimulateAsync();
        return ProtoCodec.Serialize(new TextMessage { Text = request.Text.ToUpperInvariant() });
    }

    /// <summary>Reverses the text.</summary>
    public static async Task<byte[]?> ReverseAsync(byte[]? payload)
    {
        var request = ProtoCodec.Deserialize<TextMessage>(payload);
        await Work.SimulateAsync();
        var chars = request.Text.ToCharArray();
        Array.Reverse(chars);
        return ProtoCodec.Serialize(new TextMessage { Text = new string(chars) });
    }
}