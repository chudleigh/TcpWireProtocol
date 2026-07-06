using System;
using System.Security.Cryptography;

namespace TcpWireProtocol.Samples.DiffieHellman.Shared;

/// <summary>
/// ECDH (P-256) key agreement helpers. Public keys travel over the wire protocol itself — the
/// open (zero-key) framing for the first handshake, the encrypted channel for later rekeys — and
/// both sides derive the same 32-byte AES-256 key.
/// Note: the handshake is unauthenticated. It defeats eavesdropping but not an active
/// man-in-the-middle; real use must authenticate the peer's public key (signature, certificate,
/// or an out-of-band fingerprint).
/// </summary>
public static class SecureHandshake
{
    /// <summary>Creates a fresh ephemeral key pair for one exchange.</summary>
    public static ECDiffieHellman CreateKeyPair()
    {
        return ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
    }

    /// <summary>Exports our public key in SubjectPublicKeyInfo form.</summary>
    public static byte[] ExportPublic(ECDiffieHellman ours)
    {
        return ours.ExportSubjectPublicKeyInfo();
    }

    /// <summary>Derives the shared 32-byte key from our private key and the peer's public key.</summary>
    public static byte[] DeriveKey(ECDiffieHellman ours, byte[] peerPublic)
    {
        using var peer = ECDiffieHellman.Create();
        peer.ImportSubjectPublicKeyInfo(peerPublic, out _);
        return ours.DeriveKeyFromHash(peer.PublicKey, HashAlgorithmName.SHA256);
    }

    /// <summary>A short hex fingerprint of a key, to show that a rekey changed it.</summary>
    public static string Fingerprint(byte[] key)
    {
        return Convert.ToHexString(key.AsSpan(0, 4));
    }
}