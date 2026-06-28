using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using VinceApp.Data.Models;

namespace VinceApp.Pages
{
    public partial class DashboardPage : Page
    {
        public DashboardPage()
        {
            InitializeComponent();
            this.Loaded += async (s, e) => await LoadStatisticsAsync();
        }

        private async Task LoadStatisticsAsync()
        {
            try
            {
                var stats = await Task.Run(async () =>
                {
                    using var context = new VinceSweetsDbContext();

                    DateTime now = DateTime.Now;
                    DateTime today = now.Date;
                    DateTime startOfWeek = today.AddDays(-7);
                    DateTime startOfMonth = new DateTime(now.Year, now.Month, 1);
                    DateTime startOfYear = new DateTime(now.Year, 1, 1);

                    // 1. حساب جميع الإحصائيات (يومي، أسبوعي، شهري، سنوي) في استعلام واحد
                    // قلصنا البحث ليبدأ من بداية السنة فقط لتسريع الفحص
                    var salesStats = await context.Orders
                        .AsNoTracking()
                        .Where(o => o.OrderDate >= startOfYear && !o.isDeleted && o.isPaid)
                        .GroupBy(x => 1) // تجميع كل النتائج في مجموعة واحدة
                        .Select(g => new
                        {
                            Yearly = g.Sum(o => (o.TotalAmount ?? 0) - o.DiscountAmount),
                            Monthly = g.Sum(o => o.OrderDate >= startOfMonth ? ((o.TotalAmount ?? 0) - o.DiscountAmount) : 0),
                            Weekly = g.Sum(o => o.OrderDate >= startOfWeek ? ((o.TotalAmount ?? 0) - o.DiscountAmount) : 0),
                            Daily = g.Sum(o => o.OrderDate >= today ? ((o.TotalAmount ?? 0) - o.DiscountAmount) : 0)
                        })
                        .FirstOrDefaultAsync();

                    // 2. استعلام المنتجات الأكثر مبيعاً (مبسط وأسرع)
                    var bestSellers = await context.OrderDetails
                        .AsNoTracking()
                        // استخدام Navigation Property (od.Order) بدلاً من الـ join اليدوي
                        .Where(od => !od.isDeleted && !od.Order.isDeleted && od.Order.isPaid)
                        .GroupBy(x => x.ProductName)
                        .Select(g => new
                        {
                            ProductName = g.Key,
                            Quantity = g.Sum(x => x.Quantity),
                            TotalAmount = g.Sum(x => x.Quantity * x.Price)
                        })
                        .OrderByDescending(x => x.Quantity)
                        .Take(5)
                        .ToListAsync();

                    return new
                    {
                        Daily = salesStats?.Daily ?? 0,
                        Weekly = salesStats?.Weekly ?? 0,
                        Monthly = salesStats?.Monthly ?? 0,
                        Yearly = salesStats?.Yearly ?? 0,
                        BestSellers = bestSellers
                    };
                });

                txtDailySales.Text = $"{stats.Daily:N0} د.ع";
                txtWeeklySales.Text = $"{stats.Weekly:N0} د.ع";
                txtMonthlySales.Text = $"{stats.Monthly:N0} د.ع";
                txtYearlySales.Text = $"{stats.Yearly:N0} د.ع";

                dgBestSellers.ItemsSource = stats.BestSellers;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "error with Dashboard page");
                ToastControl.Show("خطأ", "حدث خطأ أثناء تحميل الإحصائيات", ToastControl.NotificationType.Error);
            }
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadStatisticsAsync();
        }
    }
}