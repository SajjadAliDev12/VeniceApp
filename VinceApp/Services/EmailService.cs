using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using VinceApp.Data;
using VinceApp.Data.Models;
namespace VinceApp.Services
{
    public class EmailService
    {
       
        public async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                using (var context = new VinceSweetsDbContext())
                {
                    
                    var settings = await context.AppSettings.FirstOrDefaultAsync();
                    

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

                        
                        await smtpClient.SendMailAsync(mailMessage);

                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                
                throw ;
            }
            
        }
    }
}