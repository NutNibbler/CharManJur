using System.Security.Cryptography;
using System.Text;

namespace CharManJur.Services;

// Turns the key agreement result, the password key (if any) and both nonces into one key per direction.
// A wrong password gives different keys, so the first encrypted frame fails to open: that is how it is detected.
internal static class KeySchedule
{
    public static (byte[] ClientToHost, byte[] HostToClient) Derive(
        byte[] sharedSecret, byte[]? passwordKey, byte[] clientNonce, byte[] hostNonce)
    {
        var ikm = passwordKey == null ? sharedSecret : sharedSecret.Concat(passwordKey).ToArray();
        var salt = clientNonce.Concat(hostNonce).ToArray();

        var prk = HKDF.Extract(HashAlgorithmName.SHA256, ikm, salt);
        var clientToHost = HKDF.Expand(HashAlgorithmName.SHA256, prk, 32, Encoding.ASCII.GetBytes("charmanjur/v1/client-to-host"));
        var hostToClient = HKDF.Expand(HashAlgorithmName.SHA256, prk, 32, Encoding.ASCII.GetBytes("charmanjur/v1/host-to-client"));

        CryptographicOperations.ZeroMemory(prk);
        if (passwordKey != null) CryptographicOperations.ZeroMemory(ikm);
        return (clientToHost, hostToClient);
    }
}