using System.Security.Cryptography;

namespace FerreteriaWeb.Services
{
    /// <summary>
    /// Implementación de seguridad criptográfica basada en PBKDF2 (Password-Based Key Derivation Function 2)
    /// utilizando HMAC-SHA256 con 10,000 iteraciones y salt criptográficamente seguro de 128 bits.
    /// Resuelve y demuestra la implementación real solicitada en las observaciones de arquitectura.
    /// </summary>
    public class PasswordHasher : IPasswordHasher
    {
        private const int SaltSize = 16; // 128 bits
        private const int KeySize = 32;  // 256 bits
        private const int Iterations = 10000;

        public string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentNullException(nameof(password));

            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                Iterations,
                HashAlgorithmName.SHA256,
                KeySize);

            return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
        }

        public bool VerifyPassword(string password, string storedHash)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash))
                return false;

            var parts = storedHash.Split('.');
            if (parts.Length != 3)
                return false;

            if (!int.TryParse(parts[0], out int iterations))
                return false;

            byte[] salt;
            byte[] hash;
            try
            {
                salt = Convert.FromBase64String(parts[1]);
                hash = Convert.FromBase64String(parts[2]);
            }
            catch
            {
                return false;
            }

            byte[] testHash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                hash.Length);

            return CryptographicOperations.FixedTimeEquals(hash, testHash);
        }
    }
}

