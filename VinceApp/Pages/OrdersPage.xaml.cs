using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using VinceApp.Data;

namespace VinceApp.Pages
{
    public partial class OrdersPage : Page
    {
        private int _currentPage = 1;
        private int _pageSize = 20; // عدد العناصر في الصفحة
        private int _totalPages = 0;

        public OrdersPage()
        {
            InitializeComponent();
            LoadOrders();
        }

        private void LoadOrders()
        {
            try
            {
                using (var context = new VinceSweetsDbContext())
                {
                    // 1. الاستعلام الأساسي: نريد الطلبات المدفوعة فقط (أو الكل حسب رغبتك)
                    // هنا سنجلب Paid و Completed
                    var query = context.Orders
                        .Where(o => o.OrderStatus == "Paid" || o.OrderStatus == "Completed");

                    // 2. حساب عدد الصفحات
                    int totalCount = query.Count();
                    _totalPages = (int)Math.Ceiling((double)totalCount / _pageSize);
                    if (_totalPages == 0) _totalPages = 1;

                    // 3. تطبيق التقسيم (Pagination)
                    var list = query.OrderByDescending(o => o.OrderDate) // الأحدث أولاً
                                    .Skip((_currentPage - 1) * _pageSize)
                                    .Take(_pageSize)
                                    .ToList();

                    dgOrders.ItemsSource = list;
                    UpdatePaginationButtons();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ: {ex.Message}");
            }
        }

        private void UpdatePaginationButtons()
        {
            txtPageInfo.Text = $"صفحة {_currentPage} من {_totalPages}";
            btnPrev.IsEnabled = _currentPage > 1;
            btnNext.IsEnabled = _currentPage < _totalPages;
        }

        // === البحث ===
        private void Search_Click(object sender, RoutedEventArgs e)
        {
            string input = txtSearch.Text.Trim();
            if (string.IsNullOrEmpty(input))
            {
                _currentPage = 1;
                LoadOrders();
                return;
            }

            if (int.TryParse(input, out int orderNum))
            {
                using (var context = new VinceSweetsDbContext())
                {
                    // البحث المباشر يلغي التقسيم
                    var result = context.Orders
                        .Where(o => o.OrderNumber == orderNum)
                        .ToList();

                    dgOrders.ItemsSource = result;
                    txtPageInfo.Text = "نتائج البحث";
                    btnPrev.IsEnabled = false;
                    btnNext.IsEnabled = false;
                }
            }
            else
            {
                MessageBox.Show("يرجى إدخال رقم صحيح", "OK", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Text = "";
            _currentPage = 1;
            LoadOrders();
        }

        private void TxtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) Search_Click(sender, e);
        }

        // === التنقل ===
        private void PrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1) { _currentPage--; LoadOrders(); }
        }

        private void NextPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage < _totalPages) { _currentPage++; LoadOrders(); }
        }

        // === عرض التفاصيل ===
        private void ViewDetails_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                // نفتح النافذة الصغيرة التي برمجناها
                var detailsWindow = new OrderDetailsWindow(id);
                detailsWindow.ShowDialog();
            }
        }

        // === الحذف (النووي) ===
        private void DeleteOrder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                if (MessageBox.Show("تحذير خطير!\nسيتم حذف الفاتورة نهائياً من قاعدة البيانات.\nهل أنت متأكد تماماً؟",
                    "حذف نهائي", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (var context = new VinceSweetsDbContext())
                        {
                            // 1. البحث عن الطلب
                            var order = context.Orders.Include(o => o.OrderDetails).FirstOrDefault(o => o.Id == id);
                            if (order != null)
                            {
                                // 2. حذف التفاصيل أولاً (ولو أن EF يقوم بها تلقائياً إذا كانت العلاقات صحيحة)
                                context.OrderDetails.RemoveRange(order.OrderDetails);

                                // 3. حذف الطلب نفسه
                                context.Orders.Remove(order);

                                context.SaveChanges();

                                MessageBox.Show("تم الحذف.", "OK", MessageBoxButton.OK, MessageBoxImage.Information);
                                LoadOrders(); // تحديث الجدول
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"تعذر الحذف: {ex.Message}");
                    }
                }
            }
        }
    }
}