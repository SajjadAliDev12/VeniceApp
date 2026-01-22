using System.ComponentModel.DataAnnotations;

namespace VinceApp.Data.Models
{
    public class AppSetting
    {
        public int Id { get; set; }

        [Required]
        public string SmtpServer { get; set; } = "smtp.gmail.com"; // القيمة الافتراضية للجيميل

        [Required]
        public int Port { get; set; } = 587; // البورت الافتراضي

        [Required]
        [EmailAddress]
        public string SenderEmail { get; set; } // إيميلك الذي سيرسل الكود

        [Required]
        public string SenderPassword { get; set; } // رمز التطبيقات (App Password)
        [MaxLength(100)]
        public string StoreName { get; set; } = "Vince Sweets"; // اسم افتراضي

        [MaxLength(20)]
        public string StorePhone { get; set; } = "0780000000";

        [MaxLength(200)]
        public string StoreAddress { get; set; } = "Address";

        [MaxLength(500)]
        public string ReceiptFooter { get; set; } = "شكراً لزيارتكم"; // تذييل الفاتورة

        public string PrinterName { get; set; } = "Default";// اسم الطابعة الافتراضية

        public bool PrintReceiptAfterSave { get; set; } = true;
    }
}