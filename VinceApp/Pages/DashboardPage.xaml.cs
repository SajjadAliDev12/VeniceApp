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
                // تصحيح 2: فصل العمليات الحسابية
                var stats = await Task.Run(async () =>
                {
                    using var context = new VinceSweetsDbContext();

                    DateTime now = DateTime.Now;
                    DateTime today = now.Date;
                    DateTime startOfWeek = today.AddDays(-7);
                    DateTime startOfMonth = new DateTime(now.Year, now.Month, 1);
                    DateTime startOfYear = new DateTime(now.Year, 1, 1);
                    var daily = await context.Orders
                        .Where(o => o.OrderDate >= today && !o.isDeleted && o.isPaid)
                        .SumAsync(o => (o.TotalAmount ?? 0) - o.DiscountAmount);

                    var weekly = await context.Orders
                        .Where(o => o.OrderDate >= startOfWeek && !o.isDeleted && o.isPaid)
                        .SumAsync(o => (o.TotalAmount ?? 0) - o.DiscountAmount);

                    var monthly = await context.Orders
                        .Where(o => o.OrderDate >= startOfMonth && !o.isDeleted && o.isPaid)
                        .SumAsync(o => (o.TotalAmount ?? 0) - o.DiscountAmount);

                    var yearly = await context.Orders
                        .Where(o => o.OrderDate >= startOfYear && !o.isDeleted && o.isPaid)
                        .SumAsync(o => (o.TotalAmount ?? 0) - o.DiscountAmount);

                    var bestSellers = await (from od in context.OrderDetails
                                             join o in context.Orders on od.OrderId equals o.Id
                                             where !od.isDeleted && !o.isDeleted && o.isPaid == true
                                             select new
                                             {
                                                 od.ProductName,
                                                 od.Quantity,
                                                 RowTotal = od.Quantity * od.Price
                                             })
                                      .GroupBy(x => x.ProductName)
                                      .Select(g => new
                                      {
                                          ProductName = g.Key,
                                          Quantity = g.Sum(x => x.Quantity),
                                          TotalAmount = g.Sum(x => x.RowTotal)
                                      })
                                      .OrderByDescending(x => x.Quantity)
                                      .Take(5)
                                      .ToListAsync();

                    return (Daily: daily, Weekly: weekly, Monthly: monthly, Yearly: yearly, BestSellers: bestSellers);
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