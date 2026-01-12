using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using VinceApp.Data;
using VinceApp.Data.Models;
using VinceApp.Services; // للتشفير

namespace VinceApp.Pages
{
    public partial class UsersPage : Page
    {
        private int _selectedId = 0;

        public UsersPage()
        {
            InitializeComponent();
            LoadUsers();
        }

        private void LoadUsers()
        {
            try
            {
                using (var context = new VinceSweetsDbContext())
                {
                    dgUsers.ItemsSource = context.Users.ToList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ: {ex.Message}");
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Password;
            string question = txtQuestion.Text.Trim();
            string answer = txtAnswer.Text.Trim();

            if (string.IsNullOrEmpty(username) || cmbRole.SelectedItem == null)
            {
                MessageBox.Show("يرجى إدخال اسم المستخدم والصلاحية");
                return;
            }

            string role = (cmbRole.SelectedItem as ComboBoxItem).Content.ToString();
            if (_selectedId == 1 && role != "Admin")
            {
                MessageBox.Show("لا يمكن تخفيض رتبة المدير الرئيسي (Super Admin) إلى كاشير!\nيجب أن يبقى مدير للنظام.", "حماية", MessageBoxButton.OK, MessageBoxImage.Error);
                return; // إيقاف العملية
            }
            try
            {
                using (var context = new VinceSweetsDbContext())
                {
                    // فحص تكرار الاسم
                    if (context.Users.Any(u => u.Username == username && u.Id != _selectedId))
                    {
                        MessageBox.Show("اسم المستخدم هذا موجود مسبقاً!");
                        return;
                    }

                    if (_selectedId == 0) // === إضافة جديد ===
                    {
                        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(question) || string.IsNullOrEmpty(answer))
                        {
                            MessageBox.Show("كلمة المرور وسؤال الأمان مطلوبان للمستخدم الجديد");
                            return;
                        }

                        var newUser = new User
                        {
                            Username = username,
                            Role = role,
                            PasswordHash = AuthHelper.HashText(password),
                            SecurityQuestion = question,
                            SecurityAnswerHash = AuthHelper.HashText(answer)
                        };
                        context.Users.Add(newUser);
                    }
                    else // === تعديل ===
                    {
                        var user = context.Users.Find(_selectedId);
                        if (user != null)
                        {
                            user.Username = username;
                            user.Role = role;

                            // تحديث كلمة المرور فقط إذا كتب شيئاً جديداً
                            if (!string.IsNullOrEmpty(password))
                            {
                                user.PasswordHash = AuthHelper.HashText(password);
                            }

                            // تحديث سؤال الأمان فقط إذا كتب شيئاً جديداً
                            if (!string.IsNullOrEmpty(question) && !string.IsNullOrEmpty(answer))
                            {
                                user.SecurityQuestion = question;
                                user.SecurityAnswerHash = AuthHelper.HashText(answer);
                            }
                        }
                    }

                    context.SaveChanges();
                    ClearFields();
                    LoadUsers();
                    MessageBox.Show("تم الحفظ بنجاح");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ: {ex.Message}");
            }
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                using (var context = new VinceSweetsDbContext())
                {
                    var user = context.Users.Find(id);
                    if (user != null)
                    {
                        _selectedId = user.Id;
                        txtUsername.Text = user.Username;

                        // تحديد الصلاحية في القائمة
                        foreach (ComboBoxItem item in cmbRole.Items)
                        {
                            if (item.Content.ToString() == user.Role)
                            {
                                cmbRole.SelectedItem = item;
                                break;
                            }
                        }

                        // === 🛡️ كود الحماية في الواجهة 🛡️ ===
                        // إذا كان المدير الرئيسي، نغلق القائمة
                        if (user.Id == 1)
                        {
                            cmbRole.IsEnabled = false; // يمنع التغيير
                        }
                        else
                        {
                            cmbRole.IsEnabled = true; // يسمح بالتغيير للآخرين
                        }
                        // ===================================

                        txtQuestion.Text = user.SecurityQuestion;
                        txtPassword.Password = "";
                        txtAnswer.Text = "";
                    }
                }
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                // حماية المدير الرئيسي
                if (id == 1)
                {
                    MessageBox.Show("لا يمكن حذف المدير الرئيسي (Admin)!", "منع", MessageBoxButton.OK, MessageBoxImage.Stop);
                    return;
                }

                if (MessageBox.Show("هل أنت متأكد من حذف هذا المستخدم؟", "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    using (var context = new VinceSweetsDbContext())
                    {
                        var user = context.Users.Find(id);
                        if (user != null)
                        {
                            context.Users.Remove(user);
                            context.SaveChanges();
                            LoadUsers();
                            ClearFields();
                        }
                    }
                }
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e) => ClearFields();

        private void ClearFields()
        {
            _selectedId = 0;
            txtUsername.Text = "";
            txtPassword.Password = "";
            txtQuestion.Text = "";
            txtAnswer.Text = "";
            cmbRole.SelectedIndex = -1;

            cmbRole.IsEnabled = true; // ✅ إعادة تفعيل القائمة دائماً عند الضغط على "جديد"
        }
    }
}