using Serilog;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using VinceApp.Data;
using VinceApp.Data.Models; 
using VinceApp.Services;

namespace VinceApp
{
    public partial class ForgotPasswordWindow : Window
    {
        private int _foundUserId = 0;

        public ForgotPasswordWindow()
        {
            InitializeComponent();
        }

        // 1. زر إرسال الرمز (بدلاً من البحث فقط)
        private async void SendCode_Click(object sender, RoutedEventArgs e)
        {
            string email = txtEmail.Text.Trim();
            if (string.IsNullOrEmpty(email))
            {
                MessageBox.Show("أدخل البريد الإلكتروني أولاً");
                return;
            }
            Mouse.OverrideCursor = Cursors.Wait;
            ShowLoading(true, "جاري ارسال رمز الاستعادة...");
            try
            {
                
                using (var context = new VinceSweetsDbContext())
                {
                    // البحث عن المستخدم عن طريق الإيميل
                    var user = context.Users.FirstOrDefault(u => u.EmailAddress == email);

                    if (user != null)
                    {
                        if(user.Role == UserRole.Disabled)
                        {
                            MessageBox.Show("هذا الحساب معطل يرجى التواصل مع الادارة", "حساب معطل", MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        }
                        _foundUserId = user.Id;

                        // توليد رمز عشوائي (6 أرقام)
                        string tokenCode = new Random().Next(100000, 999999).ToString();

                        // حفظ الرمز في جدول التوكنز
                        var newToken = new UserToken
                        {
                            UserId = user.Id,
                            Token = tokenCode,
                            TokenType = "PasswordReset",
                            ExpiryDate = DateTime.Now.AddMinutes(15) // صلاحية 15 دقيقة
                        };

                        context.UserTokens.Add(newToken);
                        await context.SaveChangesAsync();

                        // تنسيق رسالة الإيميل
                string emailBody = $@"
                    <div style='text-align:right; font-family: Arial;'>
                        <h2>مرحباً {user.Username}</h2>
                        <p>لقد طلبت استعادة كلمة المرور لحسابك في نظام فينيسيا.</p>
                        <p>رمز التحقق الخاص بك هو:</p>
                        <h1 style='color: #2196F3; letter-spacing: 5px;'>{tokenCode}</h1>
                        <p>هذا الرمز صالح لمدة 15 دقيقة فقط.</p>
                    </div>";

                        // استدعاء الخدمة
                        var emailService = new EmailService();
                        await emailService.SendEmailAsync(user.EmailAddress, "رمز استعادة كلمة المرور", emailBody);

                        MessageBox.Show("تم إرسال رمز التحقق إلى بريدك الإلكتروني بنجاح.", "تم الإرسال", MessageBoxButton.OK, MessageBoxImage.Information);
                        //
                        pnlReset.Visibility = Visibility.Visible;
                        txtEmail.IsEnabled = false; // قفل الإيميل لمنع تغييره
                    }
                    else
                    {
                        MessageBox.Show("هذا البريد الإلكتروني غير مسجل لدينا", "خطأ");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ: \nلايمكن الاتصال بقاعدة البيانات");
                Log.Error(ex, "فشل ارسال رمز التحقق لاستعادة الحساب");
            }
            finally
            {
                Mouse.OverrideCursor = null;
                ShowLoading(false);
            }
        }

        // 2. زر تأكيد التغيير
        private async void ConfirmReset_Click(object sender, RoutedEventArgs e)
        {
            string enteredToken = txtToken.Text.Trim();
            string newPass = txtNewPass.Password;

            if (string.IsNullOrEmpty(enteredToken) || string.IsNullOrEmpty(newPass))
            {
                MessageBox.Show("يرجى إدخال الرمز وكلمة المرور الجديدة");
                return;
            }

            try
            {
                ShowLoading(true, "جاري تغيير كلمة المرور...");
                using (var context = new VinceSweetsDbContext())
                {
                    // البحث عن التوكن الصالح(غير منتهي الصلاحية ويطابق المدخل)
                    var validToken = context.UserTokens
                        .FirstOrDefault(t => t.UserId == _foundUserId
                                          && t.Token == enteredToken
                                          && t.TokenType == "PasswordReset"
                                          && t.ExpiryDate > DateTime.Now);

                    if (validToken != null)
                    {
                        //1.تحديث الباسورد
                       var user = context.Users.Find(_foundUserId);
                        if (user != null)
                        {
                            user.PasswordHash = AuthHelper.HashText(newPass);

                            //2.حذف التوكن المستخدم(لمنع استخدامه مرة أخرى)
                            context.UserTokens.Remove(validToken);

                            await context.SaveChangesAsync();
                            try
                            {
                                string subject = "تنبيه أمني: تم تغيير كلمة المرور";
                                string body = $@"
                            <div style='text-align:right; font-family: Arial;'>
                                <h2>مرحباً {user.Username}</h2>
                                <p>نود إعلامك بأنه تم تغيير كلمة المرور الخاصة بحسابك بنجاح.</p>
                                <p><strong>الوقت:</strong> {DateTime.Now.ToString("yyyy-MM-dd HH:mm")}</p>
                                <br/>
                                <p style='color: red; font-weight: bold;'>إذا لم تكن أنت من قام بهذا الإجراء، يرجى الاتصال بالإدارة فوراً لتأمين حسابك.</p>
                            </div>";

                                var emailService = new EmailService();
                                await emailService.SendEmailAsync(user.EmailAddress, subject, body);
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, "فشل ارسال رسالة الامان");
                            }
                            MessageBox.Show("تم تغيير كلمة المرور بنجاح.\nيمكنك الدخول الآن.", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                            this.Close();
                        }
                    }
                    else
                    {
                        MessageBox.Show("الرمز غير صحيح أو انتهت صلاحيته!", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "فشل تفيير الباسورد");
                MessageBox.Show($"حدث خطأ: {ex.Message}");
            }
            finally
            {
                ShowLoading(false);
            }
        }
        private void txtEmail_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (string.IsNullOrWhiteSpace(txtEmail.Text)) return; // تجاهل إذا كان فارغ
            try
            {
                // هذه الوظيفة الجاهزة تتحقق من صحة الإيميل
                var addr = new System.Net.Mail.MailAddress(textBox.Text);

                // (اختياري) إعادة الخلفية للون الأبيض إذا كان صحيحاً
                textBox.Background = Brushes.White; // System.Windows.Media.Brushes
            }
            catch
            {
                
                MessageBox.Show("الإيميل غير صحيح");
                textBox.Background = Brushes.Pink; // System.Windows.Media.Brushes
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