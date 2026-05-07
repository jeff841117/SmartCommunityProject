using Microsoft.AspNetCore.Identity;
using sql.Models;

namespace sql.Services
{
    public static class PasswordHashHelper
    {
        private static readonly PasswordHasher<account> Hasher = new();

        public static string HashPassword(account user, string password)
        {
            return Hasher.HashPassword(user, password);
        }

        public static PasswordVerificationResult VerifyPassword(account user, string providedPassword)
        {
            return Hasher.VerifyHashedPassword(user, user.password, providedPassword);
        }

        public static bool LooksHashed(string? password)
        {
            return !string.IsNullOrWhiteSpace(password) &&
                   password.StartsWith("AQAAAA", StringComparison.Ordinal);
        }
    }
}
