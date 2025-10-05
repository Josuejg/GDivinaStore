using System.Security.Cryptography;
using System.Text;

namespace GraciaDivina.Security
{
    // Guarda/hash en formato: PBKDF2$<iteraciones>$<saltB64>$<hashB64>
    public static class PasswordHasher
    {
        public static string Hash(string password, int iterations = 100_000)
        {
            using var rng = RandomNumberGenerator.Create();
            byte[] salt = new byte[16];
            rng.GetBytes(salt);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
            byte[] subkey = pbkdf2.GetBytes(32); // 256-bit

            return $"PBKDF2${iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(subkey)}";
        }

        public static bool Verify(string password, string stored)
        {
            if (string.IsNullOrWhiteSpace(stored)) return false;
            var parts = stored.Split('$');
            if (parts.Length != 4 || parts[0] != "PBKDF2") return false;

            int iterations = int.Parse(parts[1]);
            byte[] salt = Convert.FromBase64String(parts[2]);
            byte[] expected = Convert.FromBase64String(parts[3]);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
            byte[] actual = pbkdf2.GetBytes(32);

            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
    }
}
