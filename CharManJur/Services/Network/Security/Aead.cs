using System.Buffers.Binary;
using System.Security.Cryptography;

namespace CharManJur.Services;

public enum CipherKind : byte
{
    AesGcm = 1,
    ChaCha20Poly1305 = 2
}

// Authenticated encryption of one frame. The nonce is a per-direction counter that both sides count in step
// (it is never sent), so a replayed, dropped, duplicated or reordered frame fails to decrypt.
internal abstract class Aead : IDisposable
{
    public const int TagSize = 16;

    public static bool IsSupported(CipherKind kind) => kind switch
    {
        CipherKind.AesGcm => AesGcm.IsSupported,
        CipherKind.ChaCha20Poly1305 => ChaCha20Poly1305.IsSupported,
        _ => false
    };

    public static Aead Create(CipherKind kind, byte[] key) => kind switch
    {
        CipherKind.AesGcm => new AesGcmAead(key),
        CipherKind.ChaCha20Poly1305 => new ChaChaAead(key),
        _ => throw new NotSupportedException($"Unknown cipher {kind}.")
    };

    // ciphertext followed by the 16-byte tag
    public abstract byte[] Seal(ulong counter, ReadOnlySpan<byte> plaintext);

    // null when the data was altered, replayed, out of order or sealed with another key
    public abstract byte[]? Open(ulong counter, ReadOnlySpan<byte> sealedData);

    public abstract void Dispose();

    protected static void FillNonce(ulong counter, Span<byte> nonce)
    {
        nonce.Clear();
        BinaryPrimitives.WriteUInt64BigEndian(nonce[4..], counter);
    }

    private sealed class AesGcmAead : Aead
    {
        private readonly AesGcm _cipher;

        public AesGcmAead(byte[] key) => _cipher = new AesGcm(key, TagSize);

        public override byte[] Seal(ulong counter, ReadOnlySpan<byte> plaintext)
        {
            Span<byte> nonce = stackalloc byte[12];
            FillNonce(counter, nonce);

            var result = new byte[plaintext.Length + TagSize];
            _cipher.Encrypt(nonce, plaintext, result.AsSpan(0, plaintext.Length), result.AsSpan(plaintext.Length, TagSize));
            return result;
        }

        public override byte[]? Open(ulong counter, ReadOnlySpan<byte> sealedData)
        {
            if (sealedData.Length < TagSize) return null;

            Span<byte> nonce = stackalloc byte[12];
            FillNonce(counter, nonce);

            int length = sealedData.Length - TagSize;
            var plaintext = new byte[length];
            try
            {
                _cipher.Decrypt(nonce, sealedData[..length], sealedData[length..], plaintext);
                return plaintext;
            }
            catch (CryptographicException)
            {
                return null;
            }
        }

        public override void Dispose() => _cipher.Dispose();
    }

    private sealed class ChaChaAead : Aead
    {
        private readonly ChaCha20Poly1305 _cipher;

        public ChaChaAead(byte[] key) => _cipher = new ChaCha20Poly1305(key);

        public override byte[] Seal(ulong counter, ReadOnlySpan<byte> plaintext)
        {
            Span<byte> nonce = stackalloc byte[12];
            FillNonce(counter, nonce);

            var result = new byte[plaintext.Length + TagSize];
            _cipher.Encrypt(nonce, plaintext, result.AsSpan(0, plaintext.Length), result.AsSpan(plaintext.Length, TagSize));
            return result;
        }

        public override byte[]? Open(ulong counter, ReadOnlySpan<byte> sealedData)
        {
            if (sealedData.Length < TagSize) return null;

            Span<byte> nonce = stackalloc byte[12];
            FillNonce(counter, nonce);

            int length = sealedData.Length - TagSize;
            var plaintext = new byte[length];
            try
            {
                _cipher.Decrypt(nonce, sealedData[..length], sealedData[length..], plaintext);
                return plaintext;
            }
            catch (CryptographicException)
            {
                return null;
            }
        }

        public override void Dispose() => _cipher.Dispose();
    }
}