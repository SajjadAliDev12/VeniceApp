using System;
using System.Security.Cryptography;
using System.Text;

namespace VinceApp.Services
{
    public static class AuthHelper
    {
        // 🔐 Pepper ثابتة للتطبيق (لا تُخزن في قاعدة البيانات)
        // غيرها عند النشر النهائي، ولا تغيرها بعد ذلك
        private const string AppPepper = "VinceApp@2026!SecurePepper";

        // دالة لتشفير النص باستخدام SHA256 + Pepper
        public static string HashText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            // دمج النص مع الـ Pepper
            string combined = $"{text}|{AppPepper}";

            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(combined);
                var hash = sha256.ComputeHash(bytes);

                return Convert.ToBase64String(hash);
            }
        }

        // دالة للتحقق (مقارنة آمنة زمنياً)
        public static bool VerifyText(string input, string storedHash)
        {
            if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(storedHash))
                return false;

            var inputHash = HashText(input);

            return SecureEquals(inputHash, storedHash);
        }

        // 🛡️ مقارنة آمنة ضد Timing Attacks
        private static bool SecureEquals(string a, string b)
        {
            if (a.Length != b.Length)
                return false;

            int diff = 0;
            for (int i = 0; i < a.Length; i++)
            {
                diff |= a[i] ^ b[i];
            }

            return diff == 0;
        }
    }
}
