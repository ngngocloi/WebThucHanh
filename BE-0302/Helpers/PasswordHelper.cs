using System;
using System.Security.Cryptography;

namespace BE_0302.Helpers
{
    public static class PasswordHelper
    {
        public static string Hash(string password)
        {
            byte[] salt = new byte[16];
            using (var random = RandomNumberGenerator.Create()) random.GetBytes(salt);
            using (var key = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256))
            {
                byte[] data = new byte[32];
                Buffer.BlockCopy(salt, 0, data, 0, 16);
                Buffer.BlockCopy(key.GetBytes(16), 0, data, 16, 16);
                return "p1$" + Convert.ToBase64String(data);
            }
        }
        public static bool Verify(string password, string stored)
        {
            if (stored == null || password == null) return false;
            if (!stored.TrimEnd().StartsWith("p1$"))
            {
                using (var sha = SHA256.Create())
                {
                    byte[] expected = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(stored.TrimEnd()));
                    byte[] actual = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                    int difference = 0;
                    for (int i = 0; i < expected.Length; i++) difference |= expected[i] ^ actual[i];
                    return difference == 0;
                }
            }
            try
            {
                byte[] data = Convert.FromBase64String(stored.TrimEnd().Substring(3));
                if (data.Length != 32) return false;
                byte[] salt = new byte[16];
                Buffer.BlockCopy(data, 0, salt, 0, 16);
                using (var key = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256))
                {
                    byte[] actual = key.GetBytes(16);
                    int difference = 0;
                    for (int i = 0; i < 16; i++) difference |= actual[i] ^ data[i + 16];
                    return difference == 0;
                }
            }
            catch (FormatException) { return false; }
        }
    }
}
