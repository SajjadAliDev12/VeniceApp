using System;
using System.Linq;
using System.Windows;
using VinceApp.Data;
using VinceApp.Services; // لاستخدام AuthHelper

namespace VinceApp
{
    public partial class ForgotPasswordWindow : Window
    {
        private int _foundUserId = 0; // لتخزين رقم المستخدم الذي وجدناه

        public ForgotPasswordWindow()
        {
            InitializeComponent();
        }

        // 1. زر البحث
        private void Search_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text.Trim();
            if (string.IsNullOrEmpty(username))
            {
                MessageBox.Show("أدخل اسم المستخدم أولاً");
                return;
            }

            try
            {
                using (var context = new VinceSweetsDbContext())
                {
                    var user = context.Users.FirstOrDefault(u => u.Username == username);
                    if (user != null)
                    {
                        // وجدنا المستخدم
                        _foundUserId = user.Id;
                        lblQuestion.Text = user.SecurityQuestion; // عرض السؤال

                        // إظهار باقي الحقول وقفل حقل الاسم
                        pnlReset.Visibility = Visibility.Visible;
                        txtUsername.IsEnabled = false;
                    }
                    else
                    {
                        MessageBox.Show("اسم المستخدم غير موجود", "خطأ");
                        pnlReset.Visibility = Visibility.Collapsed;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ: {ex.Message}");
            }
        }

        // 2. زر التغيير
        private void ResetPassword_Click(object sender, RoutedEventArgs e)
        {
            string answer = txtAnswer.Text.Trim();
            string newPass = txtNewPass.Password;

            if (string.IsNullOrEmpty(answer) || string.IsNullOrEmpty(newPass))
            {
                MessageBox.Show("يرجى ملء جميع الحقول");
                return;
            }

            try
            {
                using (var context = new VinceSweetsDbContext())
                {
                    var user = context.Users.Find(_foundUserId);
                    if (user != null)
                    {
                        // 1. التحقق من إجابة سؤال الأمان
                        // نشفر الإجابة التي كتبها ونقارنها بالمخزنة
                        string answerHash = AuthHelper.HashText(answer);

                        if (user.SecurityAnswerHash == answerHash)
                        {
                            // الإجابة صحيحة! -> نغير الباسورد
                            user.PasswordHash = AuthHelper.HashText(newPass);
                            context.SaveChanges();

                            MessageBox.Show("تم تغيير كلمة المرور بنجاح.\nيمكنك الدخول الآن.", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                            this.Close();
                        }
                        else
                        {
                            MessageBox.Show("إجابة سؤال الأمان غير صحيحة!", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ: {ex.Message}");
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}