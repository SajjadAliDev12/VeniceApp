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
        private async void Finish_Click(object sender, RoutedEventArgs e)
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
            try
            { Mouse.OverrideCursor = Cursors.Wait;
                try
                {
                   
                    using var ctx = new VinceSweetsDbContext();


                    await ctx.Database.MigrateAsync();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "FirstRunWizard: Migration failed");
                    txtStatus.Text = "فشل الاتصال بالسيرفر أو إنشاء قاعدة البيانات. تأكد من الإعدادات.";
                    return;
                }
                try
                {
                    
                    using var ctx = new VinceSweetsDbContext();
                    bool hasAdmin = await ctx.Users.AnyAsync(u => u.Role == UserRole.Admin);
                    if (!hasAdmin)
                    {
                        var admin = new User
                        {
                            Username = adminUser,
                            PasswordHash = AuthHelper.HashText(adminPass),
                            Role = UserRole.Admin,
                            FullName = adminName,
                            EmailAddress = adminEmail,
                            IsEmailConfirmed = true,
                        };

                        await ctx.Users.AddAsync(admin);
                        await ctx.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to create initial admin");
                    txtStatus.Text = "تم إنشاء قاعدة البيانات ولكن فشل إنشاء الأدمن.";
                    return;
                }
            }
            finally { Mouse.OverrideCursor = null; }
            AppConfigService.SetActivatedFlag(true); 

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
