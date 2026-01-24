using Microsoft.Data.SqlClient;
using Serilog;
using System;
using System.IO;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VinceApp.Services;

namespace VinceApp
{
    public partial class ServerConfigWindow : Window
    {
        // ✅ ملف أساسي (قراءة فقط غالباً) داخل مجلد البرنامج
        private readonly string _baseJsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

        // ✅ ملف المستخدم (قابل للكتابة دائماً) داخل AppData
        private readonly string _userJsonPath = AppConfigService.UserConfigPath;

        public ServerConfigWindow()
        {
            InitializeComponent();
            LoadCurrentConfig();
            

        }

        private void LoadCurrentConfig()
        {
            try
            {
                // نقرأ أولاً من user config (إذا موجود) لأنه الأحدث
                string connString = ReadConnectionStringFromJson(_userJsonPath);

                // إذا ما موجود، نقرأ من الأساسي
                if (string.IsNullOrWhiteSpace(connString))
                    connString = ReadConnectionStringFromJson(_baseJsonPath);

                if (string.IsNullOrWhiteSpace(connString))
                    return;

                var builder = new SqlConnectionStringBuilder(connString);

                txtServer.Text = builder.DataSource;
                txtDatabase.Text = builder.InitialCatalog;

                if (builder.IntegratedSecurity)
                {
                    cmbAuthType.SelectedIndex = 0; // Windows Auth
                }
                else
                {
                    cmbAuthType.SelectedIndex = 1; // SQL Auth
                    txtUser.Text = builder.UserID;
                    txtPass.Password = builder.Password;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "error with serverconfig loading");
                MessageBox.Show($"خطأ في قراءة الإعدادات: {ex.Message}");
            }
        }

        private string ReadConnectionStringFromJson(string path)
        {
            try
            {
                if (!File.Exists(path)) return "";

                var json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json)) return "";

                var node = JsonNode.Parse(json);
                return node?["ConnectionStrings"]?["DefaultConnection"]?.ToString() ?? "";
            }
            catch
            {
                return "";
            }
        }

        private void cmbAuthType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (txtUser == null || txtPass == null) return;

            bool isSqlAuth = cmbAuthType.SelectedIndex == 1;
            txtUser.IsEnabled = isSqlAuth;
            txtPass.IsEnabled = isSqlAuth;
        }

        private string BuildConnectionString()
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = txtServer.Text.Trim(),
                InitialCatalog = txtDatabase.Text.Trim(),
                TrustServerCertificate = true,
                ConnectTimeout = 3
            };

            if (cmbAuthType.SelectedIndex == 0)
            {
                builder.IntegratedSecurity = true;
            }
            else
            {
                builder.IntegratedSecurity = false;
                builder.UserID = txtUser.Text.Trim();
                builder.Password = txtPass.Password;
            }

            return builder.ConnectionString;
        }

        private async void BtnTest_Click(object sender, RoutedEventArgs e)
        {
            string connString = BuildConnectionString();

            try
            {
                ShowLoading(true, "جاري الاتصال بقاعدة البيانات ....");
                Mouse.OverrideCursor = Cursors.Wait;

                using (var conn = new SqlConnection(connString))
                {
                    await conn.OpenAsync();
                    MessageBox.Show("تم الاتصال بالسيرفر بنجاح! ✅", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "error with Serverconfig testing");
                MessageBox.Show($"فشل الاتصال: ❌\n{ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
                ShowLoading(false);
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string newConnString = BuildConnectionString();

                // ✅ احفظ في ملف user config داخل AppData بدل BaseDirectory
                SaveConnectionStringToUserConfig(newConnString);

                MessageBox.Show("تم حفظ الإعدادات بنجاح ✅", "تم الحفظ", MessageBoxButton.OK, MessageBoxImage.Information);

                // ✅ لا تعيد تشغيل البرنامج هنا
                // لأن روتين أول تشغيل/بوابة الدخول راح يعيد التحقق فوراً
                // وإذا المستخدم فتح النافذة من داخل البرنامج، أيضاً ما نريد نعمل Restart غصب.
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "error with serverconfig saving");
                MessageBox.Show($"حدث خطأ أثناء الحفظ: {ex.Message}");
            }
        }
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void SaveConnectionStringToUserConfig(string connectionString)
        {
            // نقرأ user config الحالي
            JsonObject root;

            try
            {
                root = AppConfigService.ReadUserConfig();
            }
            catch
            {
                root = new JsonObject();
            }

            root["ConnectionStrings"] ??= new JsonObject();
            root["ConnectionStrings"]!["DefaultConnection"] = connectionString;

            AppConfigService.WriteUserConfig(root);
        }

        private void ShowLoading(bool show, string msg = "")
        {
            txtLoadingMsg.Text = msg;
            LoadingOverlay.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
