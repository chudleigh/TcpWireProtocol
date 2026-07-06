namespace TcpWireProtocol.Security;

/// <summary>
/// Codec factory. The protocol has exactly one wire format: AES-GCM with a deterministic
/// per-direction counter nonce (<see cref="CounterAesGcmWireCodec"/>). There is no
/// unencrypted mode: when no key is given, the well-known all-zero key
/// (<see cref="ZeroKey"/>) is used, which makes the protocol effectively open (anyone
/// with the sources can read and forge it) while keeping a single frame format and code
/// path. Pass a real key — pre-shared or produced by a key exchange (e.g. Diffie-Hellman)
/// performed by the calling code — to get actual confidentiality and integrity.
/// </summary>
public static class WireCodec
{
    /// <summary>
    /// The well-known 32-byte all-zero key ("open protocol" mode). Returns a fresh copy.
    /// </summary>
    public static byte[] ZeroKey => new byte[32];

    /// <summary>
    /// Creates the codec for one side of a single connection.
    /// </summary>
    /// <param name="direction">The direction this side sends in.</param>
    /// <param name="key">Exactly 32 bytes (AES-256); <c>null</c> means <see cref="ZeroKey"/>.</param>
    public static IWireCodec Create(WireDirection direction, byte[]? key = null)
    {
        return new CounterAesGcmWireCodec(key ?? ZeroKey, direction);
    }
}