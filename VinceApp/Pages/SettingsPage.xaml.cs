using Microsoft.Win32; // للتعامل مع SaveFileDialog
using Serilog;
using System;
using System.Drawing.Printing; // لجلب الطابعات
using System.IO;
using System.Linq;
using System.Text.Json.Nodes; // معالجة JSON
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VinceApp.Data;
using VinceApp.Data.Models;
using VinceApp.Services;

namespace VinceApp.Pages
{
    public partial class SettingsPage : Page
    {
        private readonly string _jsonFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
        public event Action<string,string> OnNotificationReqested;
        public SettingsPage()
        {
            InitializeComponent();
            LoadAllSettings();
            ApplyPermissions();
        }

        private void LoadAllSettings()
        {
            try
            {
                
                cmbPrinters.Items.Clear();
                foreach (string printer in PrinterSettings.InstalledPrinters)
                {
                    cmbPrinters.Items.Add(printer);
                }

                // 3. تحميل الإعدادات من قاعدة البيانات
                using (var context = new VinceSweetsDbContext())
                {
                    // محاولة الاتصال للتأكد أن السيرفر يعمل قبل الجلب
                    if (context.Database.CanConnect())
                    {
                        var settings = context.AppSettings.FirstOrDefault();
                        if (settings != null)
                        {
                            // البريد
                            txtSmtpServer.Text = settings.SmtpServer;
                            txtPort.Text = settings.Port.ToString();
                            txtSenderEmail.Text = settings.SenderEmail;
                            txtSenderPass.Password = settings.SenderPassword;

                            // المتجر
                            txtStoreName.Text = settings.StoreName;
                            txtStorePhone.Text = settings.StorePhone;
                            txtStoreAddress.Text = settings.StoreAddress;
                            txtReceiptFooter.Text = settings.ReceiptFooter;
                            chkAutoPrint.IsChecked = settings.PrintReceiptAfterSave;

                            if (!string.IsNullOrEmpty(settings.PrinterName))
                            {
                                cmbPrinters.SelectedItem = settings.PrinterName;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "error with Settings page Loading");
                MessageBox.Show($"حدث خطأ أثناء تحميل الإعدادات: {ex.Message}\nتأكد أن نص الاتصال صحيح أولاً.");
            }
        }

        private void ApplyPermissions()
        {
            if(CurrentUser.Role == (int)UserRole.Cashier)
            {
                tabctrlBackup.IsEnabled = false;
                TabctrlStore.Focus();
                grbxInfo.IsEnabled = false;
            }
            if (CurrentUser.Role == (int)UserRole.Manager)
            {
                tabctrlBackup.IsEnabled = false;
                TabctrlStore.Focus();
                
            }
        }
        

        // --- زر حفظ إعدادات البريد ---
        private void SaveMail_Click(object sender, RoutedEventArgs e)
        {
            SaveToDatabase(true);
        }

        // --- زر حفظ إعدادات المتجر ---
        private void SaveStore_Click(object sender, RoutedEventArgs e)
        {
            SaveToDatabase(false);
        }

        private void SaveToDatabase(bool isEmailSettings)
        {
            try
            {
                using (var context = new VinceSweetsDbContext())
                {
                    var settings = context.AppSettings.FirstOrDefault();
                    if (settings == null)
                    {
                        settings = new AppSetting();
                        context.AppSettings.Add(settings);
                    }

                    if (isEmailSettings)
                    {
                        settings.SmtpServer = txtSmtpServer.Text;
                        settings.Port = int.Parse(txtPort.Text);
                        settings.SenderEmail = txtSenderEmail.Text;
                        if (!string.IsNullOrEmpty(txtSenderPass.Password))
                            settings.SenderPassword = txtSenderPass.Password;
                    }
                    else
                    {
                        settings.StoreName = txtStoreName.Text;
                        settings.StorePhone = txtStorePhone.Text;
                        settings.StoreAddress = txtStoreAddress.Text;
                        settings.ReceiptFooter = txtReceiptFooter.Text;
                        settings.PrintReceiptAfterSave = chkAutoPrint.IsChecked == true;

                        if (cmbPrinters.SelectedItem != null)
                            settings.PrinterName = cmbPrinters.SelectedItem.ToString();
                    }

                    context.SaveChanges();

                    OnNotificationReqested?.Invoke("تم الحفظ","تم حفظ الإعدادات بنجاح");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "error with Settings page Saving");
                MessageBox.Show($"خطأ: {ex.Message}");
            }
        }
        // --- زر استعادة النسخة الاحتياطية ---
        private async Task Restore_Click2(object sender, RoutedEventArgs e)
        {
            // 1. تحذير المستخدم
            var result = MessageBox.Show(
                "⚠️ تحذير خطير!\n\nأنت على وشك استعادة قاعدة بيانات سابقة.\nسيؤدي هذا إلى حذف جميع البيانات الحالية واستبدالها بالنسخة المختارة.\n\nهل أنت متأكد تماماً؟",
                "تأكيد الاستعادة",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "SQL Backup files (*.bak)|*.bak";
                openFileDialog.Title = "اختر ملف النسخة الاحتياطية";

                if (openFileDialog.ShowDialog() == true)
                {
                    try
                    {
                        // تغيير شكل الماوس للانتظار
                        Mouse.OverrideCursor = Cursors.Wait;

                        var backupService = new BackupService();
                        backupService.RestoreDatabase(openFileDialog.FileName);

                        Mouse.OverrideCursor = null;

                        OnNotificationReqested?.Invoke("تمت استعادة البيانات", "تمت استعادة البيانات بنجاح ستتم اعادة تشغيل البرنامج");
                        await Task.Delay(3000);
                        // إعادة تشغيل البرنامج إجبارياً لتحديث البيانات في الواجهات
                        System.Diagnostics.Process.Start(Application.ResourceAssembly.Location.Replace(".dll", ".exe"));
                        Application.Current.Shutdown();
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "error with Settings page - restore database");
                        ToastControl.Show("خطأ", "فشلت عملية الاستعادة.", ToastControl.NotificationType.Error);
                        
                    }
                    finally
                    {
                        Mouse.OverrideCursor = null;
                    }
                }
            }
        }
        // --- زر النسخ الاحتياطي ---
        private void Backup_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "SQL Backup files (*.bak)|*.bak";
            saveFileDialog.FileName = $"VeniceSweets_Backup_{DateTime.Now:yyyyMMdd_HHmm}.bak";

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    var backupService = new BackupService();
                    backupService.BackupDatabase(saveFileDialog.FileName);
                    OnNotificationReqested?.Invoke("نجاح", "تم انشاء نسخة احتياطية بنجاح");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "error with settings page - backup database");
                    MessageBox.Show($"فشل النسخ الاحتياطي.\nالسبب: {ex.Message}\n\nتأكد أن SQL Server لديه صلاحية الكتابة في المجلد المختار.", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Restore_Click(object sender, RoutedEventArgs e)
        {
           Restore_Click2(sender, e);
        }
    }
}