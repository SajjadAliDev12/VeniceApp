using AutoUpdaterDotNET;
using Microsoft.Win32;
using Serilog;
using System;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes; // لمعالجة JSON
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using VinceApp.Data;
using VinceApp.Data.Models;
using VinceApp.Services;

namespace VinceApp.Pages
{
    public partial class SettingsPage : Page
    {
        public event Action<string, string> OnNotificationReqested;

        public SettingsPage()
        {
            InitializeComponent();

            // عرض الإصدار
            try
            {
                if (txtCurrentVersion != null)
                    txtCurrentVersion.Text = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "—";
            }
            catch { }

            LoadSettings();
            ApplyPermissions();
        }

        private void LoadSettings()
        {
            try
            {
                // 1. تعبئة قائمة الطابعات
                cmbPrinters.Items.Clear();
                foreach (string printer in PrinterSettings.InstalledPrinters)
                {
                    cmbPrinters.Items.Add(printer);
                }

                // =========================================================
                // 2. تحميل الإعدادات "المحلية" من ملف JSON (الخاصة بهذا الجهاز فقط)
                // =========================================================
                var localConfig = AppConfigService.ReadUserConfig(); // دالة مساعدة تقرأ الـ JSON

                // الطابعة المختارة لهذا الجهاز
                if (localConfig["PrinterName"] != null)
                {
                    string savedPrinter = localConfig["PrinterName"].ToString();
                    if (cmbPrinters.Items.Contains(savedPrinter))
                        cmbPrinters.SelectedItem = savedPrinter;
                }

                // إعدادات الصوت لهذا الجهاز
                if (localConfig["DisableSounds"] != null)
                {
                    bool disable = (bool)localConfig["DisableSounds"];
                    chkdisableSounds.IsChecked = disable;
                    ApplySoundSetting(disable);
                }
                else
                {
                    ApplySoundSetting(false);
                }

                // =========================================================
                // 3. تحميل الإعدادات "العامة" من قاعدة البيانات (الخاصة بالمتجر)
                // =========================================================
                using (var context = new VinceSweetsDbContext())
                {
                    if (context.Database.CanConnect())
                    {
                        var dbSettings = context.AppSettings.FirstOrDefault();
                        if (dbSettings != null)
                        {
                            // بيانات المتجر
                            txtStoreName.Text = dbSettings.StoreName;
                            txtStorePhone.Text = dbSettings.StorePhone;
                            txtStoreAddress.Text = dbSettings.StoreAddress;
                            txtReceiptFooter.Text = dbSettings.ReceiptFooter;
                            chkAutoPrint.IsChecked = dbSettings.PrintReceiptAfterSave;

                            // بيانات البريد
                            txtSmtpServer.Text = dbSettings.SmtpServer;
                            txtPort.Text = dbSettings.Port.ToString();
                            txtSenderEmail.Text = dbSettings.SenderEmail;
                            txtSenderPass.Password = dbSettings.SenderPassword;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load settings");
                // لا تزعج المستخدم برسالة خطأ عند الفتح، فقط سجل الخطأ
            }
        }
        private void Page_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                // هذا السطر سيمسك النافذة التي أنشأتها برمجياً ويحركها
                Window.GetWindow(this)?.DragMove();
            }
        }
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            
            Window.GetWindow(this)?.Close();
        }
        private void SaveStore_Click(object sender, RoutedEventArgs e)
        {
            // حفظ الإعدادات العامة + المحلية
            SaveSettings(isEmailSection: false);
        }

        private void SaveMail_Click(object sender, RoutedEventArgs e)
        {
            // حفظ إعدادات البريد فقط
            SaveSettings(isEmailSection: true);
        }

        private void SaveSettings(bool isEmailSection)
        {
            try
            {
                
                if (!isEmailSection)
                {
                    var config = AppConfigService.ReadUserConfig();

                    
                    if (cmbPrinters.SelectedItem != null)
                        config["PrinterName"] = cmbPrinters.SelectedItem.ToString();
                    else
                        config["PrinterName"] = null;

                    
                    bool disableSounds = chkdisableSounds.IsChecked == true;
                    config["DisableSounds"] = disableSounds;

                    
                    AppConfigService.WriteUserConfig(config);

                    
                    ApplySoundSetting(disableSounds);
                }

                // =========================================================
                // 2. حفظ الإعدادات "العامة" في قاعدة البيانات (للمتجر كله)
                // =========================================================
                using (var context = new VinceSweetsDbContext())
                {
                    var dbSettings = context.AppSettings.FirstOrDefault();
                    if (dbSettings == null)
                    {
                        dbSettings = new AppSetting();
                        context.AppSettings.Add(dbSettings);
                    }

                    if (isEmailSection)
                    {
                        // قسم البريد
                        dbSettings.SmtpServer = txtSmtpServer.Text;
                        if (int.TryParse(txtPort.Text, out int port)) dbSettings.Port = port;
                        dbSettings.SenderEmail = txtSenderEmail.Text;
                        if (!string.IsNullOrWhiteSpace(txtSenderPass.Password))
                            dbSettings.SenderPassword = txtSenderPass.Password;
                    }
                    else
                    {
                        // قسم المتجر
                        dbSettings.StoreName = txtStoreName.Text;
                        dbSettings.StorePhone = txtStorePhone.Text;
                        dbSettings.StoreAddress = txtStoreAddress.Text;
                        dbSettings.ReceiptFooter = txtReceiptFooter.Text;
                        dbSettings.PrintReceiptAfterSave = chkAutoPrint.IsChecked == true;

                        // ملاحظة هامة: لا نحفظ الطابعة هنا لأنها إعداد محلي
                    }

                    context.SaveChanges();
                }

                OnNotificationReqested?.Invoke("تم الحفظ", "تم حفظ الإعدادات بنجاح");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error saving settings");
                OnNotificationReqested?.Invoke("خطأ", "حصل خطأ عند الحفظ تأكد من ادخال كل البيانات");
                //MessageBox.Show($"حدث خطأ أثناء الحفظ","Error",MessageBoxButton.OK,MessageBoxImage.Warning);
            }
        }

        private void ApplySoundSetting(bool disableSounds)
        {
            // نخزن الحالة في ذاكرة التطبيق الحالية
            Application.Current.Properties["DisableSounds"] = disableSounds;
        }

        private void chkdisableSounds_Checked(object sender, RoutedEventArgs e)
        {
            ApplySoundSetting(true);
        }

        private void chkdisableSounds_Unchecked(object sender, RoutedEventArgs e)
        {
            ApplySoundSetting(false);
        }

        private void ApplyPermissions()
        {
            if (CurrentUser.Role == (int)UserRole.Cashier)
            {
                if (tabctrlBackup != null) tabctrlBackup.IsEnabled = false;
                if (TabctrlStore != null) TabctrlStore.Focus();
                if (grbxInfo != null) grbxInfo.IsEnabled = false;
            }
        }

        // --- التحديث ---
        private void CheckUpdate_Click(object sender, RoutedEventArgs e)
        {
            AutoUpdater.RunUpdateAsAdmin = true;
            AutoUpdater.CheckForUpdateEvent -= AutoUpdaterOnCheckForUpdateEvent;
            AutoUpdater.CheckForUpdateEvent += AutoUpdaterOnCheckForUpdateEvent;
            AutoUpdater.Start("https://raw.githubusercontent.com/SajjadAliDev12/VeniceApp/refs/heads/main/update.xml");

            if (txtUpdateStatus != null)
            {
                txtUpdateStatus.Text = "جاري التحقق من وجود تحديثات...";
                txtUpdateStatus.Foreground = Brushes.DodgerBlue;
            }
        }

        private void AutoUpdaterOnCheckForUpdateEvent(UpdateInfoEventArgs args)
        {
            if (args.Error == null)
            {
                if (args.IsUpdateAvailable)
                {
                    try
                    {
                        AutoUpdater.ShowUpdateForm(args);
                    }
                    catch (Exception ex)
                    {
                        txtUpdateStatus.Text = "حدث خطأ أثناء عرض التحديث";
                        Log.Error(ex, "Update Error");
                    }
                }
                else
                {
                    txtUpdateStatus.Text = "أنت تستخدم أحدث نسخة من التطبيق";
                    txtUpdateStatus.Foreground = Brushes.ForestGreen;
                }
            }
            else
            {
                txtUpdateStatus.Text = "فشل الاتصال بخادم التحديث";
                txtUpdateStatus.Foreground = Brushes.Firebrick;
            }
        }

        // --- النسخ الاحتياطي ---
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
                    OnNotificationReqested?.Invoke("نجاح", "تم إنشاء نسخة احتياطية بنجاح");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Backup Failed");
                    OnNotificationReqested?.Invoke("خطأ", "حصل خطأ تأكد من امتلاك الصلاحيات للكتابة على القرص");
                    //MessageBox.Show($"فشل النسخ الاحتياطي.\nتأكد من صلاحيات المجلد.\n\nالخطأ: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private void ShowLoading(bool show, string? hint = null)
        {
            LoadingOverlay.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            if (!string.IsNullOrWhiteSpace(hint))
                txtLoadingHint.Text = hint;
        }
        // --- الاستعادة ---
        private async void Restore_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "⚠️ تحذير!\nسيتم استبدال البيانات الحالية بالكامل.\nهل أنت متأكد؟",
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
                        Mouse.OverrideCursor = Cursors.Wait;
                        ShowLoading(true, "جاري استعادة قاعدة البيانات يرجى الانتظار");
                        var backupService = new BackupService();

                        // يفضل تشغيل الاستعادة في Task منفصلة لتجنب تجميد الواجهة
                        await Task.Run(() => backupService.RestoreDatabase(openFileDialog.FileName));

                        Mouse.OverrideCursor = null;
                        OnNotificationReqested?.Invoke("تم", "تمت الاستعادة. سيتم إعادة التشغيل.");
                        ShowLoading(false);
                        await Task.Delay(4000);

                        // إعادة تشغيل التطبيق
                        var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                        System.Diagnostics.Process.Start(exePath);
                        Application.Current.Shutdown();
                    }
                    catch (Exception ex)
                    {
                        Mouse.OverrideCursor = null;
                        Log.Error(ex, "Restore Failed");
                        OnNotificationReqested?.Invoke("خطأ", "فشلت الاستعادة");
                       
                    }
                    finally { Mouse.OverrideCursor = null;ShowLoading(false); }
                }
            }
        }
    }
}