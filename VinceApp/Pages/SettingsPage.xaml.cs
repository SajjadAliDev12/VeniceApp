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
using VinceApp.Services.Cloud;

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
                cmbPrinters.Items.Clear();cmbKitchinPrinters.Items.Clear();cmbIceCreamPrinters.Items.Clear();
                cmbIceCreamPrinters.Items.Add("None");
                cmbKitchinPrinters.Items.Add("None");
                cmbPrinters.Items.Add("None");
                foreach (string printer in PrinterSettings.InstalledPrinters)
                {
                    cmbPrinters.Items.Add(printer);
                    cmbKitchinPrinters.Items.Add(printer);
                    cmbIceCreamPrinters.Items.Add(printer);
                }
                
                var localConfig = AppConfigService.ReadUserConfig(); // دالة مساعدة تقرأ الـ JSON

                // الطابعة المختارة لهذا الجهاز
                if (localConfig["PrinterName"] != null)
                {
                    string savedPrinter = localConfig["PrinterName"].ToString();
                    if (cmbPrinters.Items.Contains(savedPrinter))
                        cmbPrinters.SelectedItem = savedPrinter;
                }
                if (localConfig["KitchenPrinter"] != null)
                {
                    string savedPrinter = localConfig["KitchenPrinter"].ToString();
                    // تأكد أنك تفحص وتحدد العنصر في قائمة طابعة المطبخ
                    if (cmbKitchinPrinters.Items.Contains(savedPrinter))
                        cmbKitchinPrinters.SelectedItem = savedPrinter;
                }
                if (localConfig["IceCreamPrinter"] != null)
                {
                    string savedPrinter = localConfig["IceCreamPrinter"].ToString();
                    
                    if (cmbIceCreamPrinters.Items.Contains(savedPrinter))
                        cmbIceCreamPrinters.SelectedItem = savedPrinter;
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
                            if (!string.IsNullOrEmpty(dbSettings.CloudBackupProvider))
                            {
                                // تحديد العنصر في الكومبو
                                foreach (ComboBoxItem item in cmbCloudProvider.Items)
                                {
                                    if (item.Content.ToString() == dbSettings.CloudBackupProvider)
                                    {
                                        cmbCloudProvider.SelectedItem = item;
                                        break;
                                    }
                                }
                            }

                            chkAutoCloudBackup.IsChecked = dbSettings.AutoCloudBackup;

                            // عرض حالة الاتصال
                            if (!string.IsNullOrEmpty(dbSettings.CloudRefreshToken))
                            {
                                txtCloudStatus.Text = $"متصل: {dbSettings.CloudUserEmail ?? "مستخدم"}";
                                txtCloudStatus.Foreground = Brushes.Green;
                                btnConnectCloud.Content = "فصل الحساب";
                                btnConnectCloud.Background = Brushes.Firebrick;
                            }
                            else
                            {
                                txtCloudStatus.Text = "غير متصل";
                                txtCloudStatus.Foreground = Brushes.Gray;
                                btnConnectCloud.Content = "🔗 ربط الحساب";
                                btnConnectCloud.Background = (Brush)new BrushConverter().ConvertFrom("#1976D2");
                            }
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
        private async void BtnConnectCloud_Click(object sender, RoutedEventArgs e)
        {
            
            if (btnConnectCloud.Content.ToString() == "فصل الحساب")
            {
                try
                {
                    var result = await MyConfirmDialog.ShowAsync("تأكيد فصل الحساب", "هل أنت متأكد من رغبتك في فصل الحساب السحابي؟ سيتوقف النسخ الاحتياطي التلقائي.");

                    if (!result)
                        return;

                    

                    
                    using (var context = new VinceSweetsDbContext())
                    {
                        var dbSettings = context.AppSettings.FirstOrDefault();
                        if (dbSettings != null)
                        {
                            dbSettings.CloudBackupProvider = null;
                            dbSettings.CloudUserEmail = null;
                            dbSettings.CloudRefreshToken = null; 

                            await context.SaveChangesAsync();
                        }
                    }
                    try
                    {
                        string credPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VinceApp", "token_google");

                        if (Directory.Exists(credPath))
                        {
                            // true تعني حذف المجلد ومحتوياته
                            Directory.Delete(credPath, true);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "فشل حذف ملف التوكن المحلي");
                        // لا نوقف العملية لأن الهدف الرئيسي تحقق (فصل قاعدة البيانات)
                    }
                    // إعادة تعيين الواجهة (Reset UI)
                    txtCloudStatus.Text = "غير متصل";
                    txtCloudStatus.Foreground = Brushes.Gray; // أو اللون الافتراضي لديك
                    btnConnectCloud.Content = "🔗 ربط الحساب";
                    btnConnectCloud.IsEnabled = true;

                    OnNotificationReqested?.Invoke("تم", "تم فصل الحساب بنجاح.");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error disconnecting cloud account");
                    OnNotificationReqested?.Invoke("خطأ", "حدث خطأ أثناء فصل الحساب.");
                }

                // الخروج من الدالة حتى لا ينفذ كود الربط بالأسفل
                return;
            }
            

            // التأكد من اختيار جوجل درايف
            if (cmbCloudProvider.SelectedItem is ComboBoxItem item && item.Content.ToString() != "Google Drive")
            {
                OnNotificationReqested?.Invoke("تنبيه", "الكود الحالي يدعم Google Drive فقط في هذه المرحلة.");
                return;
            }

            try
            {
                // تغيير شكل الزر ليدل على التحميل
                btnConnectCloud.Content = "جارِ الاتصال...";
                btnConnectCloud.IsEnabled = false;

                var googleService = new GoogleDriveService();

                // هذه العملية ستفتح المتصفح وتنتظر المستخدم
                var result = await googleService.AuthenticateAsync();

                if (!string.IsNullOrEmpty(result.Email))
                {
                    // الحفظ في قاعدة البيانات
                    using (var context = new VinceSweetsDbContext())
                    {
                        var dbSettings = context.AppSettings.FirstOrDefault();
                        if (dbSettings == null)
                        {
                            dbSettings = new AppSetting();
                            context.AppSettings.Add(dbSettings);
                        }

                        dbSettings.CloudBackupProvider = "Google Drive";
                        dbSettings.CloudUserEmail = result.Email;

                        // حفظ التوكن (مهم جداً للخدمة الخلفية لاحقاً)
                        if (!string.IsNullOrEmpty(result.RefreshToken))
                        {
                            dbSettings.CloudRefreshToken = result.RefreshToken;
                        }

                        await context.SaveChangesAsync();
                    }

                    // تحديث الواجهة
                    txtCloudStatus.Text = $"متصل: {result.Email}";
                    txtCloudStatus.Foreground = Brushes.Green;
                    btnConnectCloud.Content = "فصل الحساب";
                    btnConnectCloud.Background = Brushes.Firebrick;
                    btnConnectCloud.IsEnabled = true;

                    OnNotificationReqested?.Invoke("نجاح", "تم ربط حساب Google Drive بنجاح!");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Google Auth Error");
                OnNotificationReqested?.Invoke("خطأ", "فشل الربط: " + ex.Message);

                // إعادة الزر لحالته
                btnConnectCloud.Content = "🔗 ربط الحساب";
                btnConnectCloud.IsEnabled = true;
            }
        }

        private void SaveCloudSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var context = new VinceSweetsDbContext())
                {
                    var dbSettings = context.AppSettings.FirstOrDefault();
                    if (dbSettings == null)
                    {
                        dbSettings = new AppSetting();
                        context.AppSettings.Add(dbSettings);
                    }

                    // حفظ الخيارات الظاهرة
                    if (cmbCloudProvider.SelectedItem is ComboBoxItem selectedItem)
                        dbSettings.CloudBackupProvider = selectedItem.Content.ToString();

                    dbSettings.AutoCloudBackup = chkAutoCloudBackup.IsChecked == true;

                    context.SaveChanges();
                }
                OnNotificationReqested?.Invoke("تم الحفظ", "تم حفظ خيارات النسخ السحابي");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error saving cloud settings");
                OnNotificationReqested?.Invoke("خطأ", "فشل حفظ الإعدادات");
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
                    if (cmbKitchinPrinters.SelectedItem != null)
                        config["KitchenPrinter"] = cmbKitchinPrinters.SelectedItem.ToString();
                    else
                        config["KitchenPrinter"] = null;
                    if (cmbIceCreamPrinters.SelectedItem != null)
                        config["IceCreamPrinter"] = cmbIceCreamPrinters.SelectedItem.ToString();
                    else
                        config["IceCreamPrinter"] = null;

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
                        dbSettings.PrinterName = cmbPrinters.Text;
                        
                    }

                    context.SaveChanges();
                }

                OnNotificationReqested?.Invoke("تم الحفظ", "تم حفظ الإعدادات بنجاح");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error saving settings");
                OnNotificationReqested?.Invoke("خطأ", "حصل خطأ عند الحفظ تأكد من ادخال كل البيانات");
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
        private async void Backup_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "SQL Backup files (*.bak)|*.bak";
            // اسم الملف يتضمن التاريخ والوقت
            saveFileDialog.FileName = $"VeniceSweets_Backup_{DateTime.Now:yyyyMMdd_HHmm}.bak";

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    ShowLoading(true, "جارِ إنشاء النسخة الاحتياطية ورفعها للسحابة..."); // جملة تدل على الرفع

                    var backupService = new BackupService();

                    // ✅ استخدام await والدالة الجديدة Async
                    await backupService.BackupDatabaseAsync(saveFileDialog.FileName);

                    ShowLoading(false);
                    OnNotificationReqested?.Invoke("نجاح", "تمت العملية بنجاح (محلي + سحابي)");
                }
                catch (Exception ex)
                {
                    ShowLoading(false);
                    Log.Error(ex, "Backup Failed");
                    // نظهر رسالة الخطأ للمستخدم (قد تكون فشل الرفع السحابي)
                    OnNotificationReqested?.Invoke("تنبيه", ex.Message);
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

            var result = await MyConfirmDialog.ShowAsync("تأكيد الاستعادة",
                "⚠️ تحذير!\nسيتم استبدال البيانات الحالية بالكامل.\nهل أنت متأكد؟"
                );

            if (result)
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