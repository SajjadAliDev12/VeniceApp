using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;// ضروري للـ Task
using VinceApp.Data;
using VinceApp.Data.Models;
namespace VinceApp.Services
{
    public class EmailService
    {
        // 1. تغيير التوقيع إلى async Task<bool>
        // قمنا بتغيير الاسم إلى SendEmailAsync كعرف برمجي
        public async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                using (var context = new VinceSweetsDbContext())
                {
                    // يفضل استخدام FirstOrDefaultAsync لعدم تجميد الواجهة أثناء جلب البيانات
                    // إذا لم يكن متاحاً لديك، يمكنك إبقاؤه FirstOrDefault كما كان
                    var settings = await context.AppSettings.FirstOrDefaultAsync();
                    // إذا واجهت خطأ في السطر أعلاه ولم تجد المكتبة، أعده إلى: context.AppSettings.FirstOrDefault();

                    if (settings == null)
                    {
                        throw new Exception("إعدادات البريد الإلكتروني غير مضبوطة.");
                    }

                    if (string.IsNullOrEmpty(settings.SenderEmail) || string.IsNullOrEmpty(settings.SenderPassword))
                    {
                        throw new Exception("بيانات المرسل ناقصة.");
                    }

                    using (var smtpClient = new SmtpClient(settings.SmtpServer, settings.Port))
                    {
                        smtpClient.Credentials = new NetworkCredential(settings.SenderEmail, settings.SenderPassword);
                        smtpClient.EnableSsl = true;

                        var mailMessage = new MailMessage
                        {
                            From = new MailAddress(settings.SenderEmail, "Venice Sweets Support"),
                            Subject = subject,
                            Body = body,
                            IsBodyHtml = true
                        };

                        mailMessage.To.Add(toEmail);

                        // 2. استخدام دالة الإرسال غير المتزامنة مع await
                        // هذا هو السطر المسؤول عن عدم تجميد الواجهة
                        await smtpClient.SendMailAsync(mailMessage);

                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                // رمي الخطأ للواجهة ليتم عرضه
                throw ex;
            }
        }
    }
}