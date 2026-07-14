using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using VinceApp.Data.Models;
using Serilog;
using Microsoft.EntityFrameworkCore;
using VinceApp.Data;

namespace VinceApp.Toters_Feature
{
    /// <summary>
    /// Interaction logic for TotersOrdersWindow.xaml
    /// </summary>
    public partial class TotersOrdersWindow : Window
    {
        private int _currentPage = 1;
        private int _pageSize = 20;
        private int _totalPages = 0;
        private bool _isLoaded = false;
        public TotersOrdersWindow()
        {
            InitializeComponent();
            this.Loaded += async (s, e) =>
            {
                if (_isLoaded) return;
                _isLoaded = true;
                await LoadOrders();
            };
        }
        private async Task LoadOrders()
        {
            try
            {
                using (var context = new VinceSweetsDbContext())
                {
                    bool showVoided = chkShowVoided.IsChecked == true;
                    var query = context.Orders.AsNoTracking().AsQueryable(); // ✅ AsNoTracking لأننا نعرض فقط

                    if (showVoided)
                    {
                        query = query.Where(o => o.isDeleted == true && o.OrderSource == Data.Enums.Enums.OrderSource.EnToters);
                        txtPageTitle.Text = "أرشيف الفواتير الملغاة";
                    }
                    else
                    {
                        query = query.Where(o => o.isDeleted == false && o.OrderSource == Data.Enums.Enums.OrderSource.EnToters);
                        txtPageTitle.Text = "سجل الطلبات";
                    }

                    int totalCount = await query.CountAsync();
                    _totalPages = (int)Math.Ceiling((double)totalCount / _pageSize);
                    if (_totalPages == 0) _totalPages = 1;

                    // ✅ دمج الـ Select قبل ToListAsync ليتم الفلترة داخل SQL Server وليس في الرام
                    var displayList = await query.OrderByDescending(o => o.OrderDate)
                        .Skip((_currentPage - 1) * _pageSize)
                        .Take(_pageSize)
                        .Select(o => new
                        {
                            o.Id,
                            o.TotersOrderNumber,
                            o.OrderNumber,
                            o.OrderDate,
                            o.StatusText,
                            o.isDeleted,
                            OriginalTotal = o.TotalAmount ?? 0,
                            Discount = o.DiscountAmount,
                            FinalTotal = (o.TotalAmount ?? 0) - o.DiscountAmount
                        })
                        .ToListAsync();

                    dgOrders.ItemsSource = displayList;
                    UpdatePaginationButtons();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading orders");
                ToastControl.Show("خطأ", "حدث خطأ أثناء التحميل ", ToastControl.NotificationType.Error);
            }
        }

        private void UpdatePaginationButtons()
        {
            txtPageInfo.Text = $"صفحة {_currentPage} من {_totalPages}";
            btnPrev.IsEnabled = _currentPage > 1;
            btnNext.IsEnabled = _currentPage < _totalPages;
        }

        private async void chkShowVoided_Checked(object sender, RoutedEventArgs e)
        {
            _currentPage = 1;
            await LoadOrders();
        }

        private async void chkShowVoided_Unchecked(object sender, RoutedEventArgs e)
        {
            _currentPage = 1;
            await LoadOrders();
        }

        private async void Search_Click(object sender, RoutedEventArgs e)
        {
            string input = txtSearch.Text.Trim();
            if (string.IsNullOrEmpty(input))
            {
                _currentPage = 1;
                await LoadOrders();
                return;
            }

            if (int.TryParse(input, out int orderNum))
            {
                using (var context = new VinceSweetsDbContext())
                {
                    var result = await context.Orders
                        .AsNoTracking()
                        .Where(o => o.Id == orderNum && o.isPaid == true && o.OrderSource == Data.Enums.Enums.OrderSource.EnToters)
                        .Select(o => new
                        {
                            o.Id,
                            o.TotersOrderNumber,
                            o.OrderNumber,
                            o.OrderDate,
                            o.TotalAmount,
                            o.DiscountAmount,
                            o.isDeleted,
                            FinalTotal = (o.TotalAmount ?? 0) - o.DiscountAmount
                        })
                        .ToListAsync();

                    dgOrders.ItemsSource = result;
                    txtPageInfo.Text = result.Count > 0 ? "نتائج البحث" : "لا توجد نتائج";

                    if (result.Any(o => o.isDeleted))
                    {
                        ToastControl.Show("معلومات", "هذه الفاتورة محذوفة (ملغاة)", ToastControl.NotificationType.Info);
                    }

                    btnPrev.IsEnabled = false;
                    btnNext.IsEnabled = false;
                }
            }
            else
            {
                ToastControl.Show("خطأ", "يرجى ادخال رقم الفاتورة الصحيح", ToastControl.NotificationType.Info);
            }
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Text = "";
            chkShowVoided.IsChecked = false;
            _currentPage = 1;
            await LoadOrders();
        }

        private void TxtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) Search_Click(sender, e);
        }

        private async void PrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1) { _currentPage--; await LoadOrders(); }
        }

        private async void NextPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage < _totalPages) { _currentPage++; await LoadOrders(); }
        }

        private void ViewDetails_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                var detailsWindow = new OrderDetailsWindow(id);
                detailsWindow.ShowDialog();
            }
        }

        private async void DeleteOrder_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentUser.Role != (int)UserRole.Admin)
            {
                ToastControl.Show("صلاحيات", "عذراً، إلغاء الفواتير متاح للمدير فقط.", ToastControl.NotificationType.Warning);
                return;
            }

            if (sender is Button btn && btn.Tag is int id)
            {
                var parentWindow = Window.GetWindow(this) as AdminWindow;

                if (parentWindow != null)
                {
                    if (await parentWindow.ShowConfirmMessage("تأكيد", "سيتم أرشفة هذه الفاتورة ولن تحتسب في المبيعات \nهل انت متأكد؟"))
                    {
                        try
                        {
                            using (var context = new VinceSweetsDbContext())
                            {
                                // ✅ تعديل: استخدام FirstOrDefaultAsync
                                var order = await context.Orders.Where(o => o.OrderSource == Data.Enums.Enums.OrderSource.EnToters)
                                    .Include(o => o.OrderDetails)
                                    .FirstOrDefaultAsync(o => o.Id == id);

                                if (order != null)
                                {
                                    if (order.isDeleted)
                                    {
                                        ToastControl.Show("معلومات", "هذه الفاتورة محذوفة (ملغاة)", ToastControl.NotificationType.Info);
                                        return;
                                    }

                                    order.isDeleted = true;

                                    foreach (var detail in order.OrderDetails)
                                    {
                                        detail.isDeleted = true;
                                    }

                                    // ✅ تعديل: استخدام SaveChangesAsync
                                    await context.SaveChangesAsync();

                                    ToastControl.Show("معلومات", "تم الغاء الفاتورة ونقلها الى الارشيف", ToastControl.NotificationType.Success);
                                    await LoadOrders();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Error deleting order");
                            ToastControl.Show("خطأ", "فشل الغاء الفاتورة", ToastControl.NotificationType.Error);
                        }
                    }
                }
            }
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
