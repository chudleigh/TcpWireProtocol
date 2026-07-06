using ProtoBuf;

namespace TcpWireProtocol.Samples.Protobuf.Contracts;

/// <summary>The server's acknowledgement of a received <see cref="Person"/>.</summary>
[ProtoContract]
public sealed class Ack
{
    /// <summary>Whether the person was accepted.</summary>
    [ProtoMember(1)]
    public bool Ok { get; set; }

    /// <summary>Human-readable status message.</summary>
    [ProtoMember(2)]
    public string Message { get; set; } = string.Empty;
}