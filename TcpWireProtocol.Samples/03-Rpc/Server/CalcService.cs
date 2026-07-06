using System.Threading.Tasks;
using TcpWireProtocol.Samples.Rpc.Contracts;

namespace TcpWireProtocol.Samples.Rpc.Server;

/// <summary>Calculator command handlers. Request = <see cref="CalcRequest"/>; reply = <see cref="CalcResult"/>.</summary>
internal static class CalcService
{
    /// <summary>Returns the sum of the two operands.</summary>
    public static async Task<byte[]?> AddAsync(byte[]? payload)
    {
        var request = ProtoCodec.Deserialize<CalcRequest>(payload);
        await Work.SimulateAsync();
        return ProtoCodec.Serialize(new CalcResult { Value = request.A + request.B });
    }

    /// <summary>Returns the first operand minus the second.</summary>
    public static async Task<byte[]?> SubtractAsync(byte[]? payload)
    {
        var request = ProtoCodec.Deserialize<CalcRequest>(payload);
        await Work.SimulateAsync();
        return ProtoCodec.Serialize(new CalcResult { Value = request.A - request.B });
    }

    /// <summary>Returns the product of the two operands.</summary>
    public static async Task<byte[]?> MultiplyAsync(byte[]? payload)
    {
        var request = ProtoCodec.Deserialize<CalcRequest>(payload);
        await Work.SimulateAsync();
        return ProtoCodec.Serialize(new CalcResult { Value = request.A * request.B });
    }
}