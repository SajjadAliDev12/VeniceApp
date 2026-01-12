using System.ComponentModel.DataAnnotations;

namespace VinceApp.Data.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        public string Username { get; set; } // اسم المستخدم

        [Required]
        public string PasswordHash { get; set; } // كلمة المرور (مشفرة)

        public string SecurityQuestion { get; set; } // سؤال الأمان (للاستعادة)

        public string SecurityAnswerHash { get; set; } // إجابة السؤال (مشفرة أيضاً)

        public string Role { get; set; } // Admin, Cashier
    }
}