using System;
using System.Collections.Generic;
using ProtoBuf;

namespace TcpWireProtocol.Samples.Protobuf.Contracts;

/// <summary>A person record the client sends, serialized with protobuf-net.</summary>
[ProtoContract]
public sealed class Person
{
    /// <summary>Given name.</summary>
    [ProtoMember(1)]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>Family name.</summary>
    [ProtoMember(2)]
    public string LastName { get; set; } = string.Empty;

    /// <summary>Date of birth.</summary>
    [ProtoMember(3)]
    public DateTime BirthDate { get; set; }

    /// <summary>Email addresses.</summary>
    [ProtoMember(4)]
    public List<string> Emails { get; set; } = [];
}