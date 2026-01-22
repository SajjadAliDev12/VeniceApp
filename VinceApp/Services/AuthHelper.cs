using System.Security.Cryptography;
using System.Text;

namespace VinceApp.Services
{
    public static class AuthHelper
    {
        // دالة لتشفير النص باستخدام SHA256
        public static string HashText(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(text);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        // دالة للتحقق (تقارن النص المدخل مع الهاش المحفوظ)
        public static bool VerifyText(string input, string storedHash)
        {
            var inputHash = HashText(input);
            return inputHash == storedHash;
        }
    }
}