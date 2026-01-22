using Serilog;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using VinceApp.Data;
using VinceApp.Data.Models;
using VinceApp.Services;

namespace VinceApp
{
    public partial class AccountVerificationWindow : Window
    {
        private int _userId;
        private string _email;

        // نستقبل معرف المستخدم وإيميله مباشرة من شاشة تسجيل الدخول
        public AccountVerificationWindow(int userId, string email)
        {
            InitializeComponent();
            _userId = userId;
            _email = email;

            // اختياري: إرسال الكود تلقائياً عند فتح النافذة
            SendVerificationCode();
        }

        private async void SendVerificationCode()
        {
            try
            {
                ShowLoading(true, "جاري ارسال رمز التفعيل....");
                Mouse.OverrideCursor = Cursors.Wait;
                using (var context = new VinceSweetsDbContext())
                {
                    // توليد رمز عشوائي
                    string tokenCode = new Random().Next(100000, 999999).ToString();

                    // حفظ الرمز في قاعدة البيانات
                    var newToken = new UserToken
                    {
                        UserId = _userId,
                        Token = tokenCode,
                        TokenType = "EmailConfirmation", // نوع التوكن مختلف هنا
                        ExpiryDate = DateTime.Now.AddMinutes(15)
                    };

                    context.UserTokens.Add(newToken);
                    await context.SaveChangesAsync();

                    // إرسال الإيميل
                    string emailBody = $@"
                        <div style='text-align:right; font-family: Arial;'>
                            <h2>تفعيل الحساب</h2>
                            <p>رمز تفعيل حسابك هو:</p>
                            <h1 style='color: #4CAF50; letter-spacing: 5px;'>{tokenCode}</h1>
                        </div>";

                    var emailService = new EmailService();
                    await emailService.SendEmailAsync(_email, "رمز تفعيل الحساب", emailBody);

                    MessageBox.Show($"تم إرسال رمز التفعيل إلى {_email}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"فشل إرسال الرمز: {ex.Message}");
                Log.Error(ex, "فشل ارسال رمز التفعيل");
            }
            finally
            {
                Mouse.OverrideCursor = null;
                ShowLoading(false);
            }
        }

        // زر إعادة الإرسال (اختياري)
        private void ResendCode_Click(object sender, RoutedEventArgs e)
        {
            SendVerificationCode();
        }

        // زر تأكيد الكود
        private void ConfirmCode_Click(object sender, RoutedEventArgs e)
        {
            string enteredToken = txtToken.Text.Trim(); // تأكد أن اسم الحقل في XAML هو txtToken

            if (string.IsNullOrEmpty(enteredToken))
            {
                MessageBox.Show("الرجاء إدخال الرمز");
                return;
            }

            try
            {
                ShowLoading(true, "جاري تأكيد الحساب.....");
                Mouse.OverrideCursor = Cursors.Wait;
                using (var context = new VinceSweetsDbContext())
                {
                    // البحث عن التوكن
                    var validToken = context.UserTokens
                        .FirstOrDefault(t => t.UserId == _userId
                                          && t.Token == enteredToken
                                          && t.TokenType == "EmailConfirmation"
                                          && t.ExpiryDate > DateTime.Now);

                    if (validToken != null)
                    {
                        // 1. تفعيل الحساب
                        var user = context.Users.Find(_userId);
                        if (user != null)
                        {
                            user.IsEmailConfirmed = true;

                            // 2. حذف التوكن المستخدم
                            context.UserTokens.Remove(validToken);

                            context.SaveChanges();

                            this.DialogResult = true; // إرجاع true للنافذة السابقة
                            this.Close();
                        }
                    }
                    else
                    {
                        MessageBox.Show("الرمز غير صحيح أو منتهي الصلاحية", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ: {ex.Message}");
                Log.Error(ex, "فشل التفعيل");
            }
            finally
            {
                Mouse.OverrideCursor = null;
                ShowLoading(false);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ShowLoading(bool show, string msg = "")
        {
            txtLoadingMsg.Text = msg;
            LoadingOverlay.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}