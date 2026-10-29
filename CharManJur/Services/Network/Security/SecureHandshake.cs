using System.Buffers.Binary;
using System.Security.Cryptography;

namespace CharManJur.Services;

public enum HandshakeFailure
{
    None,
    BadPassword,
    PasswordRequired,
    RateLimited,
    Incompatible,
    Protocol
}

public sealed record HandshakeResult(IConnection? Connection, HandshakeFailure Failure, string Message)
{
    public bool Succeeded => Connection != null;
}

public sealed class HandshakeOptions
{
    // In order of preference. A cipher the device cannot run is skipped, so a phone without AES hardware support still connects.
    public CipherKind[] Ciphers { get; set; } = { CipherKind.AesGcm, CipherKind.ChaCha20Poly1305 };
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);
    // For a player that lives in the same app as the host (the GM's test players): the password's derived key, used
    // instead of a typed password. Never set this for anything that crosses a real network.
    public byte[]? PasswordKey { get; set; }
}

// L1: runs on a fresh raw connection BEFORE any session traffic and returns the same link, now encrypted.
//
//   client -> host   ClientHello  "CMJ1", ciphers it can run, its ephemeral ECDH P-256 key, a random nonce
//   host   -> client ServerHello  "CMJ2", chosen cipher, password needed?, salt + iterations, its ECDH key, a nonce
//   both             keys = HKDF(ECDH secret + password key, nonces): one per direction
//   client -> host   Finished     sealed: 'C' + hash of both hellos      (host opens it: wrong password fails HERE)
//   host   -> client Finished     sealed: 'S' + the same hash
//   (a refusal is a plain 5-byte frame: "CMJX" + a code)
//
// Always encrypted (ECDH). The password adds authentication: without it a man in the middle could not be told
// from the host. The password itself is never sent; only a value derived from it is mixed into the keys.
public static class SecureHandshake
{
    private static readonly byte[] ClientMagic = { (byte)'C', (byte)'M', (byte)'J', (byte)'1' };
    private static readonly byte[] ServerMagic = { (byte)'C', (byte)'M', (byte)'J', (byte)'2' };
    private static readonly byte[] FailureMagic = { (byte)'C', (byte)'M', (byte)'J', (byte)'X' };
    private const int NonceBytes = 32;
    private const byte ClientLabel = (byte)'C';
    private const byte ServerLabel = (byte)'S';

    // ---------------------------------------------------------------- client

    public static async Task<HandshakeResult> ConnectAsync(
        IConnection raw, string? password, HandshakeOptions? options = null, CancellationToken ct = default)
    {
        options ??= new HandshakeOptions();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(options.Timeout);

        try
        {
            var offered = options.Ciphers.Where(Aead.IsSupported).Distinct().ToArray();
            if (offered.Length == 0) return Fail(HandshakeFailure.Incompatible, "This device cannot run any encryption the app supports.");

            using var ecdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
            var clientNonce = RandomNumberGenerator.GetBytes(NonceBytes);
            var hello = BuildClientHello(offered, ecdh.PublicKey.ExportSubjectPublicKeyInfo(), clientNonce);
            try
            {
                await raw.SendAsync(hello, timeout.Token).ConfigureAwait(false);
            }
            catch (IOException)
            {
                // A host that is refusing us (e.g. locked out) may hang up before our hello lands; its reason may still be waiting to be read.
            }

            var serverFrame = await raw.ReceiveAsync(timeout.Token).ConfigureAwait(false);
            if (serverFrame == null) return Fail(HandshakeFailure.Protocol, "The host closed the connection during the handshake.");
            if (TryReadFailure(serverFrame, out var refused)) return refused;

            if (!TryParseServerHello(serverFrame, out var server))
                return Fail(HandshakeFailure.Protocol, "The host's reply was not understood. Are both apps the same version?");

            if (!offered.Contains(server.Cipher))
                return Fail(HandshakeFailure.Incompatible, "The host chose an encryption this device did not offer.");

            if (server.PasswordRequired)
            {
                if (string.IsNullOrEmpty(password) && options.PasswordKey == null)
                    return Fail(HandshakeFailure.PasswordRequired, "This session needs a password.");
                if (options.PasswordKey == null && server.Iterations is < SessionCredentials.MinIterations or > SessionCredentials.MaxIterations)
                    return Fail(HandshakeFailure.Protocol, "The host asked for an unreasonable password setting.");
            }

            // CHANGED: a key handed in by the host's own app replaces the typed password.
            byte[]? passwordKey = null;
            if (server.PasswordRequired)
            {
                passwordKey = options.PasswordKey != null
                    ? (byte[])options.PasswordKey.Clone()   // a copy: it is wiped below
                    : await Task.Run(() => SessionCredentials.Derive(password!, server.Salt!, server.Iterations), timeout.Token).ConfigureAwait(false);
            }

            using var hostKey = ECDiffieHellman.Create();
            hostKey.ImportSubjectPublicKeyInfo(server.PublicKey, out _);
            var secret = ecdh.DeriveRawSecretAgreement(hostKey.PublicKey);

            var (clientToHost, hostToClient) = KeySchedule.Derive(secret, passwordKey, clientNonce, server.Nonce);
            CryptographicOperations.ZeroMemory(secret);
            if (passwordKey != null) CryptographicOperations.ZeroMemory(passwordKey);

            var send = Aead.Create(server.Cipher, clientToHost);
            var receive = Aead.Create(server.Cipher, hostToClient);

            var transcript = SHA256.HashData(hello.Concat(serverFrame).ToArray());
            await raw.SendAsync(send.Seal(0, Labelled(ClientLabel, transcript)), timeout.Token).ConfigureAwait(false);

            var reply = await raw.ReceiveAsync(timeout.Token).ConfigureAwait(false);
            if (reply == null) return Fail(HandshakeFailure.Protocol, "The host closed the connection during the handshake.");
            if (TryReadFailure(reply, out refused)) return refused;

            var opened = receive.Open(0, reply);
            if (opened == null || !Matches(opened, ServerLabel, transcript))
                return Fail(HandshakeFailure.Protocol, "The host could not be verified.");

            return new HandshakeResult(new EncryptedConnection(raw, send, receive, 1, 1), HandshakeFailure.None, string.Empty);
        }
        catch (OperationCanceledException)
        {
            return Fail(HandshakeFailure.Protocol, "The handshake timed out.");
        }
        catch (Exception ex) when (ex is IOException or CryptographicException or ArgumentException or FormatException)
        {
            return Fail(HandshakeFailure.Protocol, "The handshake failed: " + ex.Message);
        }
    }

    // ---------------------------------------------------------------- host

    // credentials == null: no password. limiter may be null (no lockout).
    public static async Task<HandshakeResult> AcceptAsync(
        IConnection raw, SessionCredentials? credentials, AuthLimiter? limiter, HandshakeOptions? options = null, CancellationToken ct = default)
    {
        options ??= new HandshakeOptions();
        string peer = AuthLimiter.KeyFor(raw.RemoteDescription);

        if (limiter != null && limiter.IsBlocked(peer))
        {
            await TrySendFailureAsync(raw, FailureCode.RateLimited).ConfigureAwait(false);
            return Fail(HandshakeFailure.RateLimited, "Too many wrong passwords. Try again in a minute.");
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(options.Timeout);

        try
        {
            var clientFrame = await raw.ReceiveAsync(timeout.Token).ConfigureAwait(false);
            if (clientFrame == null) return Fail(HandshakeFailure.Protocol, "The connection closed before the handshake.");

            if (!TryParseClientHello(clientFrame, out var client))
            {
                await TrySendFailureAsync(raw, FailureCode.Protocol).ConfigureAwait(false);
                return Fail(HandshakeFailure.Protocol, "The first message was not a valid hello.");
            }

            CipherKind? chosen = options.Ciphers.Where(Aead.IsSupported).Cast<CipherKind?>()
                .FirstOrDefault(c => client.Ciphers.Contains(c!.Value));
            if (chosen == null)
            {
                await TrySendFailureAsync(raw, FailureCode.Incompatible).ConfigureAwait(false);
                return Fail(HandshakeFailure.Incompatible, "No encryption in common.");
            }

            using var ecdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
            var hostNonce = RandomNumberGenerator.GetBytes(NonceBytes);
            var serverHello = BuildServerHello(chosen.Value, credentials, ecdh.PublicKey.ExportSubjectPublicKeyInfo(), hostNonce);

            using var clientKey = ECDiffieHellman.Create();
            clientKey.ImportSubjectPublicKeyInfo(client.PublicKey, out _);
            var secret = ecdh.DeriveRawSecretAgreement(clientKey.PublicKey);

            var (clientToHost, hostToClient) = KeySchedule.Derive(secret, credentials?.Key, client.Nonce, hostNonce);
            CryptographicOperations.ZeroMemory(secret);

            var send = Aead.Create(chosen.Value, hostToClient);
            var receive = Aead.Create(chosen.Value, clientToHost);

            await raw.SendAsync(serverHello, timeout.Token).ConfigureAwait(false);

            var finished = await raw.ReceiveAsync(timeout.Token).ConfigureAwait(false);
            if (finished == null)
                return Fail(HandshakeFailure.Protocol, "The player disconnected during the handshake.");   // e.g. no password to give: not a guess

            var transcript = SHA256.HashData(clientFrame.Concat(serverHello).ToArray());
            var opened = receive.Open(0, finished);
            if (opened == null || !Matches(opened, ClientLabel, transcript))
            {
                if (credentials != null)
                {
                    limiter?.RecordFailure(peer);
                    await TrySendFailureAsync(raw, FailureCode.BadPassword).ConfigureAwait(false);
                    return Fail(HandshakeFailure.BadPassword, "Wrong password.");
                }

                await TrySendFailureAsync(raw, FailureCode.Protocol).ConfigureAwait(false);
                return Fail(HandshakeFailure.Protocol, "The handshake could not be verified.");
            }

            limiter?.RecordSuccess(peer);
            await raw.SendAsync(send.Seal(0, Labelled(ServerLabel, transcript)), timeout.Token).ConfigureAwait(false);
            return new HandshakeResult(new EncryptedConnection(raw, send, receive, 1, 1), HandshakeFailure.None, string.Empty);
        }
        catch (OperationCanceledException)
        {
            return Fail(HandshakeFailure.Protocol, "The handshake timed out.");
        }
        catch (Exception ex) when (ex is IOException or CryptographicException or ArgumentException or FormatException)
        {
            await TrySendFailureAsync(raw, FailureCode.Protocol).ConfigureAwait(false);
            return Fail(HandshakeFailure.Protocol, "The handshake failed: " + ex.Message);
        }
    }

    // ---------------------------------------------------------------- frames

    private enum FailureCode : byte { BadPassword = 1, RateLimited = 2, Incompatible = 3, Protocol = 4 }

    private sealed record ClientHello(CipherKind[] Ciphers, byte[] PublicKey, byte[] Nonce);

    private sealed record ServerHello(CipherKind Cipher, bool PasswordRequired, byte[]? Salt, int Iterations, byte[] PublicKey, byte[] Nonce);

    private static byte[] BuildClientHello(CipherKind[] ciphers, byte[] publicKey, byte[] nonce)
    {
        var frame = new List<byte>();
        frame.AddRange(ClientMagic);
        frame.Add((byte)ciphers.Length);
        foreach (var cipher in ciphers) frame.Add((byte)cipher);
        AddBlock16(frame, publicKey);
        frame.AddRange(nonce);
        return frame.ToArray();
    }

    private static bool TryParseClientHello(byte[] frame, out ClientHello hello)
    {
        hello = null!;
        var reader = new FrameReader(frame);
        if (!reader.ReadMagic(ClientMagic)) return false;

        if (!reader.TryReadByte(out var count) || count == 0 || count > 8) return false;
        var ciphers = new CipherKind[count];
        for (int i = 0; i < count; i++)
        {
            if (!reader.TryReadByte(out var id)) return false;
            ciphers[i] = (CipherKind)id;
        }

        if (!reader.TryReadBlock16(out var publicKey) || !reader.TryRead(NonceBytes, out var nonce) || !reader.AtEnd) return false;

        hello = new ClientHello(ciphers, publicKey, nonce);
        return true;
    }

    private static byte[] BuildServerHello(CipherKind cipher, SessionCredentials? credentials, byte[] publicKey, byte[] nonce)
    {
        var frame = new List<byte>();
        frame.AddRange(ServerMagic);
        frame.Add((byte)cipher);
        frame.Add((byte)(credentials != null ? 1 : 0));
        if (credentials != null)
        {
            frame.Add((byte)credentials.Salt.Length);
            frame.AddRange(credentials.Salt);
            var iterations = new byte[4];
            BinaryPrimitives.WriteInt32BigEndian(iterations, credentials.Iterations);
            frame.AddRange(iterations);
        }
        AddBlock16(frame, publicKey);
        frame.AddRange(nonce);
        return frame.ToArray();
    }

    private static bool TryParseServerHello(byte[] frame, out ServerHello hello)
    {
        hello = null!;
        var reader = new FrameReader(frame);
        if (!reader.ReadMagic(ServerMagic)) return false;
        if (!reader.TryReadByte(out var cipher) || !reader.TryReadByte(out var flags)) return false;

        bool required = (flags & 1) != 0;
        byte[]? salt = null;
        int iterations = 0;
        if (required)
        {
            if (!reader.TryReadByte(out var saltLength) || !reader.TryRead(saltLength, out salt)) return false;
            if (!reader.TryRead(4, out var iterationBytes)) return false;
            iterations = BinaryPrimitives.ReadInt32BigEndian(iterationBytes);
        }

        if (!reader.TryReadBlock16(out var publicKey) || !reader.TryRead(NonceBytes, out var nonce) || !reader.AtEnd) return false;

        hello = new ServerHello((CipherKind)cipher, required, salt, iterations, publicKey, nonce);
        return true;
    }

    private static async Task TrySendFailureAsync(IConnection raw, FailureCode code)
    {
        try
        {
            await raw.SendAsync(FailureMagic.Append((byte)code).ToArray()).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException) { }
    }

    // A refusal is exactly 5 bytes ("CMJX" + code); every genuine hello or sealed frame is longer.
    private static bool TryReadFailure(byte[] frame, out HandshakeResult result)
    {
        result = null!;
        if (frame.Length != 5 || !frame.AsSpan(0, 4).SequenceEqual(FailureMagic)) return false;

        result = (FailureCode)frame[4] switch
        {
            FailureCode.BadPassword => Fail(HandshakeFailure.BadPassword, "Wrong password."),
            FailureCode.RateLimited => Fail(HandshakeFailure.RateLimited, "Too many wrong passwords. Try again in a minute."),
            FailureCode.Incompatible => Fail(HandshakeFailure.Incompatible, "Your app and the GM's app are different versions. Update the app on both devices."),
            _ => Fail(HandshakeFailure.Protocol, "The host refused the connection.")
        };
        return true;
    }

    private static byte[] Labelled(byte label, byte[] hash) => new[] { label }.Concat(hash).ToArray();

    private static bool Matches(byte[] opened, byte label, byte[] transcript)
        => opened.Length == transcript.Length + 1 && opened[0] == label
           && CryptographicOperations.FixedTimeEquals(opened.AsSpan(1), transcript);

    private static void AddBlock16(List<byte> frame, byte[] block)
    {
        frame.Add((byte)(block.Length >> 8));
        frame.Add((byte)block.Length);
        frame.AddRange(block);
    }

    private static HandshakeResult Fail(HandshakeFailure failure, string message) => new(null, failure, message);

    // Bounds-checked reader: hostile or truncated hellos return false instead of throwing.
    private sealed class FrameReader
    {
        private readonly byte[] _data;
        private int _position;

        public FrameReader(byte[] data) => _data = data;

        public bool AtEnd => _position == _data.Length;

        public bool ReadMagic(byte[] magic) => TryRead(magic.Length, out var read) && read.AsSpan().SequenceEqual(magic);

        public bool TryReadByte(out byte value)
        {
            value = 0;
            if (_position >= _data.Length) return false;
            value = _data[_position++];
            return true;
        }

        public bool TryRead(int count, out byte[] bytes)
        {
            bytes = Array.Empty<byte>();
            if (count < 0 || _data.Length - _position < count) return false;
            bytes = _data.AsSpan(_position, count).ToArray();
            _position += count;
            return true;
        }

        public bool TryReadBlock16(out byte[] bytes)
        {
            bytes = Array.Empty<byte>();
            if (!TryRead(2, out var length)) return false;
            int size = (length[0] << 8) | length[1];
            return size > 0 && size <= 256 && TryRead(size, out bytes);
        }
    }
}