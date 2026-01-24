using AutoUpdaterDotNET;
using Serilog;
using System;
using System.Data.SqlClient;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace VinceKitchen
{
    public partial class SettingsWindow : Window
    {
        private static readonly string AppDataDir =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VinceKitchen");

        private static readonly string ConfigFilePath =
            Path.Combine(AppDataDir, "appsettings.user.json");

        public SettingsWindow()
        {
            InitializeComponent();
            LoadSettings();
            txtCurrentVersion.Text = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";

        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var builder = new SqlConnectionStringBuilder
                {
                    DataSource = ServerPathTxt.Text.Trim(),
                    InitialCatalog = DbNameTxt.Text.Trim(),
                    TrustServerCertificate = true,
                    ConnectTimeout = 3
                };

                if (UseAuthCheck.IsChecked == true)
                {
                    builder.IntegratedSecurity = false;
                    builder.UserID = UserIdTxt.Text.Trim();
                    builder.Password = PasswordTxt.Password;
                }
                else
                {
                    builder.IntegratedSecurity = true;
                }

                var config = new AppConfig
                {
                    ConnectionStrings = new ConnectionStringsWrapper
                    {
                        DefaultConnection = builder.ConnectionString
                    }
                };

                Directory.CreateDirectory(AppDataDir);

                var options = new JsonSerializerOptions { WriteIndented = true };
                string jsonString = JsonSerializer.Serialize(config, options);
                File.WriteAllText(ConfigFilePath, jsonString);

                MessageBox.Show("تم حفظ الإعدادات بنجاح!\n" + ConfigFilePath,
                    "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);

                Close();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Kitchen settings saving error");
                MessageBox.Show($"حدث خطأ أثناء الحفظ:\n{ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void update_Click(object sender, RoutedEventArgs e)
        {
            AutoUpdater.RunUpdateAsAdmin = true;
            AutoUpdater.CheckForUpdateEvent -= AutoUpdaterOnCheckForUpdateEvent;
            AutoUpdater.CheckForUpdateEvent += AutoUpdaterOnCheckForUpdateEvent;

            AutoUpdater.Start("https://raw.githubusercontent.com/SajjadAliDev12/VeniceApp/refs/heads/main/KitchenUpdate.xml");
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
                        MessageBox.Show(ex.Message, "خطأ في التحديث", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    MessageBox.Show("أنت تستخدم أحدث إصدار حالياً.", "معلومات", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show("حدث مشكلة أثناء البحث عن التحديثات، تأكد من الاتصال بالانترنت.", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void LoadSettings()
        {
            try
            {
                if (!File.Exists(ConfigFilePath)) return;

                string jsonString = File.ReadAllText(ConfigFilePath);
                var config = JsonSerializer.Deserialize<AppConfig>(jsonString);

                var cs = config?.ConnectionStrings?.DefaultConnection;
                if (string.IsNullOrWhiteSpace(cs)) return;

                var builder = new SqlConnectionStringBuilder(cs);

                ServerPathTxt.Text = builder.DataSource;
                DbNameTxt.Text = builder.InitialCatalog;

                if (builder.IntegratedSecurity)
                {
                    UseAuthCheck.IsChecked = false;
                    UserIdTxt.Text = "";
                    PasswordTxt.Password = "";
                }
                else
                {
                    UseAuthCheck.IsChecked = true;
                    UserIdTxt.Text = builder.UserID;
                    PasswordTxt.Password = builder.Password;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Kitchen settings loading error");
                MessageBox.Show($"فشل في تحميل الإعدادات السابقة:\n{ex.Message}", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

    public class AppConfig
    {
        public ConnectionStringsWrapper ConnectionStrings { get; set; } = new ConnectionStringsWrapper();
    }

    public class ConnectionStringsWrapper
    {
        public string DefaultConnection { get; set; } = "";
    }
}
