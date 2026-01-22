using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VinceApp.Data.Models
{
    public class UserToken
    {
        public int Id { get; set; }

        [Required]
        public string Token { get; set; } // الرمز المشفر أو GUID

        [Required]
        public DateTime ExpiryDate { get; set; } // تاريخ الانتهاء

        [Required]
        [MaxLength(50)]
        public string TokenType { get; set; } // نوع التوكن: "EmailActivation" أو "PasswordReset"

        // Foreign Key
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; }
    }
}