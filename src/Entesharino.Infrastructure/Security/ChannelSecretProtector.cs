using System.Security.Cryptography;
using System.Text;
using Entesharino.Application.Common.Interfaces;

namespace Entesharino.Infrastructure.Security;

public sealed class ChannelSecretProtector : ISecretProtector
{
    private const int KeySize = 32;
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private readonly byte[] _key;

    public ChannelSecretProtector(IConfiguration configuration)
    {
        var configuredKey = configuration["Security:ChannelEncryptionKey"];

        if (string.IsNullOrWhiteSpace(configuredKey))
            throw new InvalidOperationException(
                "Security:ChannelEncryptionKey is not configured.");

        _key = SHA256.HashData(Encoding.UTF8.GetBytes(configuredKey));
    }

    public string Protect(string value)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));

        var plaintext = Encoding.UTF8.GetBytes(value);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(_key, TagSize);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        var result = new byte[NonceSize + TagSize + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, result, 0, NonceSize);
        Buffer.BlockCopy(tag, 0, result, NonceSize, TagSize);
        Buffer.BlockCopy(ciphertext, 0, result, NonceSize + TagSize, ciphertext.Length);

        return Convert.ToBase64String(result);
    }

    public string Unprotect(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Encrypted value cannot be empty.", nameof(value));

        var data = Convert.FromBase64String(value);

        if (data.Length < NonceSize + TagSize)
            throw new CryptographicException("Invalid encrypted value.");

        var nonce = data.AsSpan(0, NonceSize);
        var tag = data.AsSpan(NonceSize, TagSize);
        var ciphertext = data.AsSpan(NonceSize + TagSize);
        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(_key, TagSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);

        return Encoding.UTF8.GetString(plaintext);
    }
}
