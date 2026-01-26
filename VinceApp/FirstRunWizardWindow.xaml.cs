using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Windows;
using System.Windows.Input;
using VinceApp.Data;
using VinceApp.Data.Models;
using VinceApp.Services;

namespace VinceApp
{
    public partial class FirstRunWizardWindow : Window
    {
        public FirstRunWizardWindow()
        {
            InitializeComponent();
        }

        private void OpenServerSettings_Click(object sender, RoutedEventArgs e)
        {
            // ✅ إعادة استخدام نافذتك الحالية
            var w = new ServerConfigWindow();
            w.ShowDialog();
        }
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }
        private void Finish_Click(object sender, RoutedEventArgs e)
        {
            txtStatus.Text = "";

            // 1. التحقق من مفتاح التفعيل
            var key = txtActivationKey.Password?.Trim();
            if (string.IsNullOrWhiteSpace(key) || !ValidateActivationKey(key))
            {
                txtStatus.Text = "رمز التفعيل غير صحيح.";
                return;
            }

            // 2. التحقق من بيانات الأدمن (Validation)
            string adminUser = txtAdminUsername.Text.Trim();
            string adminPass = txtAdminPassword.Password;
            string adminName = txtAdminFullName.Text.Trim();
            string adminEmail = txtAdminEmail.Text.Trim();

            if (string.IsNullOrWhiteSpace(adminUser) || string.IsNullOrWhiteSpace(adminPass) ||
                string.IsNullOrWhiteSpace(adminName) || string.IsNullOrWhiteSpace(adminEmail))
            {
                txtStatus.Text = "يرجى إدخال جميع بيانات الأدمن.";
                return;
            }

            // =========================================================
            //  الخطوة الحاسمة: إنشاء قاعدة البيانات والجداول (Migration)
            // =========================================================
            try
            {
                using var ctx = new VinceSweetsDbContext();

                // هذا الأمر سينشئ الداتابيس إذا لم تكن موجودة + ينشئ الجداول
                // إذا فشل الاتصال بالسيرفر، سيقع في الـ catch
                ctx.Database.Migrate();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "FirstRunWizard: Migration failed");
                txtStatus.Text = "فشل الاتصال بالسيرفر أو إنشاء قاعدة البيانات. تأكد من الإعدادات.";
                return; // نخرج ولا نكمل
            }

            // =========================================================
            //  إنشاء حساب الأدمن (الآن الجداول موجودة ومضمونة)
            // =========================================================
            try
            {
                using var ctx = new VinceSweetsDbContext();

                // نتأكد لربما يوجد أدمن سابقاً (حالة نادرة)
                bool hasAdmin = ctx.Users.Any(u => u.Role == UserRole.Admin);
                if (!hasAdmin)
                {
                    var admin = new User
                    {
                        Username = adminUser,
                        PasswordHash = AuthHelper.HashText(adminPass), // تأكد أن دالة التشفير تعمل
                        Role = UserRole.Admin,
                        FullName = adminName,
                        EmailAddress = adminEmail,
                        IsEmailConfirmed = true,
                    };

                    ctx.Users.Add(admin);
                    ctx.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to create initial admin");
                txtStatus.Text = "تم إنشاء قاعدة البيانات ولكن فشل إنشاء الأدمن.";
                return;
            }

            // 3. إنهاء العملية
            AppConfigService.SetActivatedFlag(true); // حفظ أن البرنامج تم تفعيله

            MessageBox.Show("تم إعداد النظام بنجاح! يمكنك تسجيل الدخول الآن.", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);

            this.DialogResult = true;
            this.Close();
        }

        private bool ValidateActivationKey(string key)
        {
            // ✅ هنا تربطها بنظام التفعيل الحقيقي لاحقاً
            // حالياً مثال: أي مفتاح طوله >= 10
            return key.Length >= 10;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
