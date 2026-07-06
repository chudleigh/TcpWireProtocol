using System.IO;
using ProtoBuf;

namespace TcpWireProtocol.Samples.Rpc.Contracts;

/// <summary>Serializes protobuf-net contract objects to byte payloads and back.</summary>
public static class ProtoCodec
{
    /// <summary>Serializes a contract object to a byte payload.</summary>
    public static byte[] Serialize<T>(T value)
    {
        using var buffer = new MemoryStream();
        Serializer.Serialize(buffer, value);
        return buffer.ToArray();
    }

    /// <summary>Deserializes a byte payload back into a contract object (an empty payload yields defaults).</summary>
    public static T Deserialize<T>(byte[]? payload)
    {
        using var buffer = new MemoryStream(payload ?? []);
        return Serializer.Deserialize<T>(buffer);
    }
}