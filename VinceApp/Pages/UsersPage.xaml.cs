using Serilog;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using VinceApp.Data;
using VinceApp.Data.Models;
using VinceApp.Services;

namespace VinceApp.Pages
{
    public partial class UsersPage : Page
    {
        private int _selectedId = 0;

        public UsersPage()
        {
            InitializeComponent();

            // 1. تعبئة القائمة تلقائياً من الـ Enum لضمان تطابق القيم
            // يجب إزالة العناصر اليدوية من ملف XAML إذا كانت موجودة داخل الـ ComboBox
            cmbRole.ItemsSource = Enum.GetValues(typeof(UserRole));

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
                Log.Error(ex, "error with users page loading");
                ToastControl.Show("Error", "حدث خطأ ", ToastControl.NotificationType.Error);
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // 1. جلب البيانات الجديدة
            string username = txtUsername.Text.Trim();
            string email = txtEmail.Text.Trim(); // حقل جديد بدلاً من السؤال
            string password = txtPassword.Password;
            string FullName = txtFullName.Text.Trim();

            // التحقق من المدخلات الأساسية
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || cmbRole.SelectedItem == null || string.IsNullOrEmpty(FullName))
            {
                ToastControl.Show("خطأ", "يرجى إدخال اسم المستخدم، الإيميل، والصلاحية والاسم", ToastControl.NotificationType.Info);
                
                return;
            }

            UserRole selectedRole = (UserRole)cmbRole.SelectedItem;

            // حماية الـ Admin
            if (_selectedId == 1 && selectedRole != UserRole.Admin)
            {
                ToastControl.Show("خطأ", "لا يمكن تغيير صلاحية المدير الرئيسي!", ToastControl.NotificationType.Warning);
                
                return;
            }

            try
            {
                using (var context = new VinceSweetsDbContext())
                {
                    // 2. التحقق من التكرار (اسم المستخدم أو الإيميل)
                    if (context.Users.Any(u => (u.Username == username || u.EmailAddress == email || u.FullName == FullName) && u.Id != _selectedId))
                    {
                        ToastControl.Show("خطأ", "اسم المستخدم او الايميل موجود مسبقاً!", ToastControl.NotificationType.Warning);
                        return;
                    }

                    if (_selectedId == 0) // === إضافة جديد ===
                    {
                        if (string.IsNullOrEmpty(password))
                        {
                            ToastControl.Show("خطأ", "كلمة المرور مطلوبة للمستخدم الجديد", ToastControl.NotificationType.Warning);
                            
                            return;
                        }

                        var newUser = new User
                        {
                            Username = username,
                            EmailAddress = email, 
                            Role = selectedRole,
                            FullName = FullName,
                            PasswordHash = AuthHelper.HashText(password),
                            IsEmailConfirmed = false 
                        };
                        context.Users.Add(newUser);
                    }
                    else 
                    {
                        var user = context.Users.Find(_selectedId);
                        if (user != null)
                        {
                            user.Username = username;
                            user.EmailAddress = email; 
                            user.Role = selectedRole;
                            user.FullName = FullName;
                            
                            if (!string.IsNullOrEmpty(password))
                            {
                                user.PasswordHash = AuthHelper.HashText(password);
                            }

                            
                            if (_selectedId == CurrentUser.Id)
                                CurrentUser.Role = (int)selectedRole;
                        }
                    }

                    context.SaveChanges();
                    ClearFields();
                    LoadUsers();
                    ToastControl.Show("تم الحفظ", "تم الحفظ بنجاح", ToastControl.NotificationType.Success);
                    
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "error with users page saving");
                ToastControl.Show("خطأ", "حدث خطأ في البرنامج", ToastControl.NotificationType.Warning);
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
                        txtEmail.Text = user.EmailAddress; // جلب الإيميل
                        cmbRole.SelectedItem = (UserRole)user.Role;
                        txtFullName.Text = user.FullName;
                        // تفريغ الباسورد لأنه مشفر ولا يعرض
                        txtPassword.Password = "";

                        // التحكم في صلاحية تعديل الرتبة
                        cmbRole.IsEnabled = (user.Id != 1);
                    }
                }
            }
        }

        private void ClearFields()
        {
            _selectedId = 0;
            txtUsername.Text = "";
            txtEmail.Text = ""; // تفريغ الإيميل
            txtPassword.Password = "";
            cmbRole.SelectedIndex = -1;
            cmbRole.IsEnabled = true;
            txtFullName.Text = "";
        }


        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                if (id == 1)
                {

                    ToastControl.Show("منع", "لا يمكن حذف المدير الرئيسي (Admin)!", ToastControl.NotificationType.Error);
                    return;
                }
                var parentWindow = Window.GetWindow(this) as AdminWindow;

                if (parentWindow != null)
                {
                    if (await parentWindow.ShowConfirmMessage("تأكيد", "هل أنت متأكد من حذف هذا المستخدم؟"))
                    {
                        using (var context = new VinceSweetsDbContext())
                        {
                            // ... كود الحذف القديم ...
                            var user = context.Users.Find(id);
                            if (user != null)
                            {
                                context.Users.Remove(user);
                                context.SaveChanges();
                                LoadUsers();
                                ClearFields();
                                ToastControl.Show("تم الحذف", "تم الحذف بنجاح", ToastControl.NotificationType.Success);
                            }
                        }
                    }
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e) => ClearFields();
    }
}