using Serilog;
using System;
using System.Linq;
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
            LoadStatistics();
        }

        private void LoadStatistics()
        {
            try
            {
                using (var context = new VinceSweetsDbContext())
                {
                    DateTime now = DateTime.Now;
                    DateTime today = now.Date; // الساعة 00:00 اليوم
                    DateTime startOfWeek = today.AddDays(-7); // آخر 7 أيام
                    DateTime startOfMonth = new DateTime(now.Year, now.Month, 1); // أول يوم في الشهر
                    DateTime startOfYear = new DateTime(now.Year, 1, 1); // أول يوم في السنة

                    // --- 1. حساب المبيعات للفترات الزمنية ---

                    // جلب كل الطلبات لتجنب استعلامات متكررة (إذا كانت البيانات ضخمة جداً يفضل فصلها)
                    // ملاحظة: نستخدم TotalAmount ?? 0 لأن الحقل Nullable

                    // مبيعات اليوم
                    var dailySum = context.Orders
                        .Where(o => o.OrderDate >= today && o.isDeleted == false)
                        .Sum(o => (o.TotalAmount ?? 0) - o.DiscountAmount);

                    // مبيعات الأسبوع (آخر 7 أيام)
                    var weeklySum = context.Orders
                        .Where(o => o.OrderDate >= startOfWeek && o.isDeleted == false)
                        .Sum(o => (o.TotalAmount ?? 0) - o.DiscountAmount);

                    // مبيعات الشهر الحالي
                    var monthlySum = context.Orders
                        .Where(o => o.OrderDate >= startOfMonth && o.isDeleted == false)
                        .Sum(o => (o.TotalAmount ?? 0) - o.DiscountAmount);

                    // مبيعات السنة الحالية
                    var yearlySum = context.Orders
                        .Where(o => o.OrderDate >= startOfYear && o.isDeleted == false)
                        .Sum(o => (o.TotalAmount ?? 0) - o.DiscountAmount);

                    // عرض البيانات في البطاقات
                    txtDailySales.Text = $"{dailySum:N0} د.ع";
                    txtWeeklySales.Text = $"{weeklySum:N0} د.ع";
                    txtMonthlySales.Text = $"{monthlySum:N0} د.ع";
                    txtYearlySales.Text = $"{yearlySum:N0} د.ع";


                    // --- 2. حساب الأكثر مبيعاً (Top Sellers) ---
                    // نذهب لجدول التفاصيل، نجمع حسب اسم المنتج، ونرتب تنازلياً حسب الكمية
                    // --- 2. حساب الأكثر مبيعاً (Top Sellers) ---
                    var bestSellers = (
                        from od in context.OrderDetails
                        join o in context.Orders on od.OrderId equals o.Id
                        // تأكد من أن السعر (Price) موجود في جدول التفاصيل
                        // إذا كان الحقل اسمه مختلف (مثلاً UnitPrice) يرجى تعديله هنا
                        where !od.isDeleted && !o.isDeleted
                        select new
                        {
                            od.ProductName,
                            od.Quantity,
                            // نحسب اجمالي السطر الواحد هنا (الكمية * السعر)
                            RowTotal = od.Quantity * od.Price
                        }
                    )
                    .GroupBy(x => x.ProductName)
                    .Select(g => new
                    {
                        ProductName = g.Key,
                        // جمع الكميات
                        Quantity = g.Sum(x => x.Quantity),
                        // جمع المبالغ
                        TotalAmount = g.Sum(x => x.RowTotal)
                    })
                    .OrderByDescending(x => x.Quantity)
                    .Take(5)
                    .ToList();

                    dgBestSellers.ItemsSource = bestSellers;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "error with Dashboard page");
                ToastControl.Show( "خطأ","حدث خطأ أثناء تحميل الإحصائيات", ToastControl.NotificationType.Error);
                
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadStatistics();
        }
    }
}