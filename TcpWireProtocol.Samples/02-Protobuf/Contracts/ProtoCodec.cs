using System.IO;
using ProtoBuf;

namespace TcpWireProtocol.Samples.Protobuf.Contracts;

/// <summary>
/// Turns protobuf-net contract objects into byte payloads and back, round-tripping null:
/// a null object returns a null payload, and a null (or empty) payload deserializes back to null.
/// </summary>
public static class ProtoCodec
{
    /// <summary>Serializes a contract object to a byte payload; a null object returns a null payload.</summary>
    public static byte[]? Serialize<T>(T? value)
    {
        if (value is null) { return null; }

        using var buffer = new MemoryStream();
        Serializer.Serialize(buffer, value);
        return buffer.ToArray();
    }

    /// <summary>Deserializes a byte payload back into a contract object; a null or empty payload yields null.</summary>
    public static T? Deserialize<T>(byte[]? payload)
    {
        if (payload is null || payload.Length == 0) { return default; }

        using var buffer = new MemoryStream(payload);
        return Serializer.Deserialize<T>(buffer);
    }
}