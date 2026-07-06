using ProtoBuf;

namespace TcpWireProtocol.Samples.Rpc.Contracts;

/// <summary>Operands for a Calc command.</summary>
[ProtoContract]
public sealed class CalcRequest
{
    /// <summary>First operand.</summary>
    [ProtoMember(1)]
    public int A { get; set; }

    /// <summary>Second operand.</summary>
    [ProtoMember(2)]
    public int B { get; set; }
}

/// <summary>Result of a Calc command.</summary>
[ProtoContract]
public sealed class CalcResult
{
    /// <summary>The computed value.</summary>
    [ProtoMember(1)]
    public int Value { get; set; }
}

/// <summary>Text carried by a Text command's request and reply.</summary>
[ProtoContract]
public sealed class TextMessage
{
    /// <summary>The text.</summary>
    [ProtoMember(1)]
    public string Text { get; set; } = string.Empty;
}