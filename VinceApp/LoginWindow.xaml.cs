using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VinceApp.Data;
using VinceApp.Data.Models;
using VinceApp.Services;

namespace VinceApp
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            txtUser.Focus();
            

        }

        private async void Login_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUser.Text.Trim();
            string password = txtPass.Password;

            // التحقق من المدخلات
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("يرجى إدخال البيانات كاملة", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // تعطيل الزر وتغيير الماوس لوضع الانتظار
            var btn = sender as Button;
            if (btn != null) btn.IsEnabled = false;
            Mouse.OverrideCursor = Cursors.Wait;

            try
            {
                using (var context = new VinceSweetsDbContext())
                {
                    ShowLoading(true, "جاري تسجيل الدخول...");
                    string inputHash = AuthHelper.HashText(password);

                    // البحث عن المستخدم (بشكل غير متزامن لتجنب تجميد الواجهة)
                    var user = await context.Users
                        .FirstOrDefaultAsync(u => u.Username == username && u.PasswordHash == inputHash);

                    if (user == null)
                    {
                        MessageBox.Show("اسم المستخدم او كلمة المرور خاطئة", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // التحقق من حالة الحساب
                    if (user.Role == (UserRole)UserRole.Disabled)
                    {
                        MessageBox.Show("هذا الحساب معطل، يرجى مراجعة الإدارة.", "محظور", MessageBoxButton.OK, MessageBoxImage.Stop);
                        return;
                    }

                    if (!user.IsEmailConfirmed)
                    {
                        // نخفي مؤشر الانتظار مؤقتاً لظهور الـ MessageBox بشكل طبيعي
                        Mouse.OverrideCursor = null;

                        var result = MessageBox.Show("هذا الحساب غير مفعل. هل تريد إرسال رمز التفعيل الآن؟",
                                                     "تفعيل الحساب",
                                                     MessageBoxButton.YesNo,
                                                     MessageBoxImage.Question);

                        if (result == MessageBoxResult.Yes)
                        {
                            ShowLoading(false);
                            AccountVerificationWindow verifyWindow = new AccountVerificationWindow(user.Id, user.EmailAddress);
                            bool? isVerified = verifyWindow.ShowDialog();

                            if (isVerified == true)
                            {
                                MessageBox.Show("تم تفعيل الحساب بنجاح، يمكنك تسجيل الدخول الآن.", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                        }
                        return;
                    }
                    
                    // تسجيل الدخول وحفظ الجلسة
                    CurrentUser.Id = user.Id;
                    CurrentUser.Username = user.Username;
                    CurrentUser.Role = (int)user.Role;
                    CurrentUser.FullName = user.FullName;

                    this.DialogResult = true;
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "خطأ في شاشة تسجيل الدخول");
                MessageBox.Show($"حدث خطأ في الاتصال: \nتأكد من ان قاعدة البيانات تعمل وتم ضبط الاتصال في اعلى صفحه تسجيل الدخول", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ShowLoading(false);
                if (btn != null) btn.IsEnabled = true;
                Mouse.OverrideCursor = null;
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void BtnServerSettings_Click(object sender, RoutedEventArgs e)
        {
            ServerConfigWindow configWindow = new ServerConfigWindow();
            configWindow.ShowDialog();
        }
        private void ForgotPassword_Click(object sender, RoutedEventArgs e)
        {
            ForgotPasswordWindow forgot = new ForgotPasswordWindow();
            forgot.ShowDialog();
        }
        private void ShowLoading(bool show, string msg = "")
        {
            txtLoadingMsg.Text = msg;
            LoadingOverlay.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}