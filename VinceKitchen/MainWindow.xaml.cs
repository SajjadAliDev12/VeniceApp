using AutoUpdaterDotNET;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using VinceApp.Data.Models;

namespace VinceKitchen
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer _timer;
        private bool _isBusy = false;

        // ✅ نفس مكان حفظ الإعدادات في SettingsWindow
        private static readonly string AppDataDir =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VinceKitchen");

        private static readonly string ConfigFilePath =
            Path.Combine(AppDataDir, "appsettings.user.json");

        public MainWindow()
        {
            InitializeComponent();

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(5);
            _timer.Tick += async (s, e) => await LoadKitchenOrders();
            _timer.Start();

            _ = LoadKitchenOrders();

            AutoUpdater.RunUpdateAsAdmin = true;
            AutoUpdater.Start("https://raw.githubusercontent.com/SajjadAliDev12/VeniceApp/refs/heads/main/KitchenUpdate.xml");
        }

        // ✅ قراءة ConnectionString من appsettings.user.json (إن وجد)
        private static string? TryGetUserConnectionString()
        {
            try
            {
                if (!File.Exists(ConfigFilePath)) return null;

                string json = File.ReadAllText(ConfigFilePath);
                using var doc = JsonDocument.Parse(json);

                if (doc.RootElement.TryGetProperty("ConnectionStrings", out var csNode) &&
                    csNode.TryGetProperty("DefaultConnection", out var connNode))
                {
                    var conn = connNode.GetString();
                    return string.IsNullOrWhiteSpace(conn) ? null : conn;
                }

                return null;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to read kitchen user settings (appsettings.user.json)");
                return null;
            }
        }

        // ✅ إنشاء DbContext حسب الإعدادات (بدون كسر المنطق الحالي)
        private static VinceSweetsDbContext CreateDbContext()
        {
            var userConn = TryGetUserConnectionString();

            if (!string.IsNullOrWhiteSpace(userConn))
            {
                var optionsBuilder = new DbContextOptionsBuilder<VinceSweetsDbContext>();
                optionsBuilder.UseSqlServer(userConn);
                return new VinceSweetsDbContext(optionsBuilder.Options);
            }

            // fallback: الإعدادات الافتراضية كما كانت
            return new VinceSweetsDbContext();
        }

        private async Task LoadKitchenOrders()
        {
            if (_isBusy) return;
            _isBusy = true;

            try
            {
                var displayList = await Task.Run(async () =>
                {
                    using (var context = CreateDbContext())
                    {
                        var tables = await context.RestaurantTables.ToListAsync();

                        var activeOrders = await context.Orders
                            .Include(o => o.OrderDetails)
                            .ThenInclude(d => d.Product)
                            .Where(o => (o.isPaid || o.isSentToKitchen)
                                && o.OrderDetails.Any(d => d.Product.IsKitchenItem == true && !d.IsServed && !d.isDeleted))
                            .OrderBy(o => o.OrderDate)
                            .ToListAsync();

                        var list = new List<KitchenOrderViewModel>();

                        foreach (var order in activeOrders)
                        {
                            var pendingItems = order.OrderDetails
                                .Where(d => d.Product.IsKitchenItem == true && !d.IsServed && !d.isDeleted)
                                .Select(d => new KitchenItemView
                                {
                                    ProductName = d.ProductName,
                                    Quantity = d.Quantity
                                })
                                .ToList();

                            if (pendingItems.Any())
                            {
                                var tableObj = tables.FirstOrDefault(t => t.Id == order.TableId);
                                string tableName = tableObj != null ? $"طاولة {tableObj.TableNumber}" : "📦 سفري";
                                if (order.ParentOrderId != null)
                                    tableName += " - ملحق";

                                list.Add(new KitchenOrderViewModel
                                {
                                    OrderId = order.Id,
                                    TableNumberDisplay = tableName,
                                    OrderTime = order.OrderDate ?? DateTime.Now,
                                    KitchenItems = pendingItems
                                });
                            }
                        }

                        return list;
                    }
                });

                OrdersList.ItemsSource = displayList;

                if (StatusBorder != null) StatusBorder.Background = Brushes.LimeGreen;

                // ✅ اختياري: إذا موجود بالتصميم الجديد
                if (txtConnectionStatus != null) txtConnectionStatus.Text = "متصل";
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in kitchen screen LoadOrders()");
                if (StatusBorder != null) StatusBorder.Background = Brushes.Red;

                // ✅ اختياري: إذا موجود بالتصميم الجديد
                if (txtConnectionStatus != null) txtConnectionStatus.Text = "غير متصل";
            }
            finally
            {
                _isBusy = false;
            }
        }

        private async void OrderDone_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(
                "الطلب جاهز\nسوف يتم حذف هذا الطلب من قائمة الانتظار",
                "تأكيد",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Information) != MessageBoxResult.OK)
                return;

            try
            {
                // ✅ Tag ممكن يجي int أو string أو long .. نخليه آمن
                if (sender is not Button btn || btn.Tag == null) return;

                if (!int.TryParse(btn.Tag.ToString(), out int orderId)) return;

                bool success = await Task.Run(async () =>
                {
                    using (var context = CreateDbContext())
                    {
                        var order = await context.Orders
                            .Include(o => o.OrderDetails)
                            .ThenInclude(d => d.Product)
                            .FirstOrDefaultAsync(o => o.Id == orderId);

                        if (order == null) return false;

                        var itemsToServe = order.OrderDetails
                            .Where(d => d.Product.IsKitchenItem == true && !d.IsServed && !d.isDeleted)
                            .ToList();

                        foreach (var item in itemsToServe)
                            item.IsServed = true;

                        order.isReady = true;
                        await context.SaveChangesAsync();
                        return true;
                    }
                });

                if (success)
                    await LoadKitchenOrders();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "OrderDone_Click error");
                MessageBox.Show("فشل الاتصال بالسيرفر.", "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
                if (StatusBorder != null) StatusBorder.Background = Brushes.Red;
                if (txtConnectionStatus != null) txtConnectionStatus.Text = "غير متصل";
            }
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow settingsWin = new SettingsWindow();
            settingsWin.Owner = this;
            settingsWin.ShowDialog();
        }
    }

    public class KitchenOrderViewModel
    {
        public int OrderId { get; set; }
        public string TableNumberDisplay { get; set; }
        public DateTime OrderTime { get; set; }
        public List<KitchenItemView> KitchenItems { get; set; }

        // ✅ القديم (خليه إذا XAML القديم/الحالي يستخدمه)
        public string TimeDisplay
        {
            get
            {
                var diff = DateTime.Now - OrderTime;
                if (diff.TotalHours < 1)
                    return $"{diff.Minutes} دقيقة";
                else
                    return $"{(int)diff.TotalHours} س و {diff.Minutes} د";
            }
        }

       
        public string ElapsedDisplay
        {
            get
            {
                var diff = DateTime.Now - OrderTime;
                if (diff.TotalHours < 1)
                    return $"{diff.Minutes:0}د";
                return $"{(int)diff.TotalHours}س {diff.Minutes:0}د";
            }
        }

        
        public bool IsLate
        {
            get
            {
                var diff = DateTime.Now - OrderTime;
                return diff.TotalMinutes >= 20;
            }
        }
    }

    public class KitchenItemView
    {
        public string ProductName { get; set; }
        public int Quantity { get; set; }
    }
}
