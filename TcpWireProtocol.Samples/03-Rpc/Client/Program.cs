using System;
using System.Threading.Tasks;
using TcpWireProtocol.Samples.Common;

using TcpWireProtocol.Samples.Rpc.Contracts;

namespace TcpWireProtocol.Samples.Rpc.Client;

/// <summary>
/// Command client: on each Enter, fires a batch of calc and text commands concurrently over one
/// connection. Replies may arrive out of order, but each is matched back to its call by CmdId.
/// </summary>
internal static class Program
{
    /// <summary>Connects, then sends a batch of commands on each Enter until end of input.</summary>
    private static async Task Main()
    {
        var port = int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var p) ? p : 5000;

        using var tcp = await WireClient.ConnectWithRetryAsync("127.0.0.1", port);
        using var conn = FramedConnection.Client(tcp.GetStream());
        var channel = new CommandChannel(conn);

        Console.WriteLine("press Enter to send a batch of commands (replies arrive out of order, matched by CmdId; Ctrl+C to quit)");
        while (Console.ReadLine() is not null)
        {
            await Task.WhenAll(
                CalcAsync(channel, "2 + 3", CalcCommands.ADD, 2, 3),
                CalcAsync(channel, "10 - 4", CalcCommands.SUBTRACT, 10, 4),
                CalcAsync(channel, "6 * 7", CalcCommands.MULTIPLY, 6, 7),
                TextAsync(channel, "upper", TextCommands.UPPER, "hello wire"),
                TextAsync(channel, "reverse", TextCommands.REVERSE, "hello wire"));
        }
    }

    /// <summary>Calls a Calc command with two operands and prints the int result.</summary>
    private static async Task CalcAsync(CommandChannel channel, string label, short command, int a, int b)
    {
        var reply = await channel.CallAsync(Services.CALC, command, ProtoCodec.Serialize(new CalcRequest { A = a, B = b }));
        Console.WriteLine($"{label} = {ProtoCodec.Deserialize<CalcResult>(reply).Value}");
    }

    /// <summary>Calls a Text command with a string and prints the transformed string.</summary>
    private static async Task TextAsync(CommandChannel channel, string label, short command, string text)
    {
        var reply = await channel.CallAsync(Services.TEXT, command, ProtoCodec.Serialize(new TextMessage { Text = text }));
        Console.WriteLine($"{label} = {ProtoCodec.Deserialize<TextMessage>(reply).Text}");
    }
}