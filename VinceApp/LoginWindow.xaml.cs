using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using VinceApp.Data; // تأكد أن هذا الـ Namespace صحيح حسب مشروع الداتا
using VinceApp.Services; // تأكد أن AuthHelper موجود هنا

namespace VinceApp
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            txtUser.Focus(); // وضع المؤشر في خانة الاسم تلقائياً
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUser.Text.Trim();
            string password = txtPass.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("يرجى إدخال البيانات كاملة", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var context = new VinceSweetsDbContext())
                {
                    // 1. تشفير كلمة المرور المدخلة لتطابق التشفير في الداتا بيس
                    string inputHash = AuthHelper.HashText(password);
                    
                    // 2. البحث عن المستخدم
                    var user = context.Users
                        .FirstOrDefault(u => u.Username == username && u.PasswordHash == inputHash);
                    if(user != null && user.Role.ToLower() != "admin")
                    {
                        MessageBox.Show("ليس لديك الصلاحية للدخول الى هذه الصفحة","Error",MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    if (user != null && user.Role.ToLower() == "admin")
                    {

                        AdminWindow admin = new AdminWindow();
                        admin.Show();

                        this.Close();
                    }
                    else
                    {
                        MessageBox.Show("اسم المستخدم او كلمة المرور خاطئة", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ في الاتصال: {ex.Message}");
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ForgotPassword_Click(object sender, RoutedEventArgs e)
        {
            ForgotPasswordWindow forgot = new ForgotPasswordWindow();
            forgot.ShowDialog();
        }
    }
}