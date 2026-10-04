using System.Security.Cryptography;
using System.Text;

namespace NoFences.Model
{
    /// <summary>
    /// Password protection for notes: AES-256-GCM with a key derived from the password (PBKDF2-SHA256).
    /// Without the password the text can't be recovered – not even by NoFences.
    /// </summary>
    public static class NoteCrypto
    {
        private const string Prefix = "v1:";
        private const int SaltSize = 16, NonceSize = 12, TagSize = 16, Iterations = 200_000;

        public static string Encrypt(string text, string password)
        {
            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var nonce = RandomNumberGenerator.GetBytes(NonceSize);
            var plain = Encoding.UTF8.GetBytes(text);
            var cipher = new byte[plain.Length];
            var tag = new byte[TagSize];
            using (var aes = new AesGcm(Key(password, salt), TagSize))
                aes.Encrypt(nonce, plain, cipher, tag);
            return Prefix + Convert.ToBase64String(salt.Concat(nonce).Concat(tag).Concat(cipher).ToArray());
        }

        /// <summary>The text, or null if the password is wrong (or the data damaged).</summary>
        public static string? Decrypt(string data, string password)
        {
            try
            {
                if (!data.StartsWith(Prefix, StringComparison.Ordinal))
                    return null;
                var bytes = Convert.FromBase64String(data[Prefix.Length..]);
                if (bytes.Length < SaltSize + NonceSize + TagSize)
                    return null;
                var salt = bytes[..SaltSize];
                var nonce = bytes[SaltSize..(SaltSize + NonceSize)];
                var tag = bytes[(SaltSize + NonceSize)..(SaltSize + NonceSize + TagSize)];
                var cipher = bytes[(SaltSize + NonceSize + TagSize)..];
                var plain = new byte[cipher.Length];
                using var aes = new AesGcm(Key(password, salt), TagSize);
                aes.Decrypt(nonce, cipher, tag, plain);
                return Encoding.UTF8.GetString(plain);
            }
            catch (Exception e) when (e is CryptographicException or FormatException)
            {
                return null;
            }
        }

        private static byte[] Key(string password, byte[] salt) =>
            Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, Iterations, HashAlgorithmName.SHA256, 32);
    }
}
