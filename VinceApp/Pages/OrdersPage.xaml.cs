using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VinceApp.Data;
using VinceApp.Data.Models;

namespace VinceApp.Pages
{
    public partial class OrdersPage : Page
    {
        private int _currentPage = 1;
        private int _pageSize = 20;
        private int _totalPages = 0;

        public OrdersPage()
        {
            InitializeComponent();
            // تأكد من وجود CheckBox في الـ XAML باسم chkShowVoided
            // وربط حدث Checked/Unchecked بهذا: chkShowVoided_CheckedChanged
            LoadOrders();
        }

        private void LoadOrders()
        {
            try
            {
                using (var context = new VinceSweetsDbContext())
                {
                    bool showVoided = chkShowVoided.IsChecked == true;
                    var query = context.Orders.AsQueryable();

                    if (showVoided)
                    {
                        query = query.Where(o => o.isDeleted == true);
                        txtPageTitle.Text = "أرشيف الفواتير الملغاة";
                    }
                    else
                    {
                        query = query.Where(o => o.isDeleted == false);
                        txtPageTitle.Text = "سجل الطلبات";
                    }

                    int totalCount = query.Count();
                    _totalPages = (int)Math.Ceiling((double)totalCount / _pageSize);
                    if (_totalPages == 0) _totalPages = 1;

                    var rawList = query.OrderByDescending(o => o.OrderDate)
                                       .Skip((_currentPage - 1) * _pageSize)
                                       .Take(_pageSize)
                                       .ToList();

                    // === التعديل هنا ===
                    var displayList = rawList.Select(o => new
                    {
                        o.Id,
                        o.OrderNumber,
                        o.OrderDate,
                        o.StatusText,

                        // تم إزالة (?? 0) من الخصم لأنه لا يقبل Null
                        OriginalTotal = o.TotalAmount ?? 0,
                        Discount = o.DiscountAmount,
                        FinalTotal = (o.TotalAmount ?? 0) - o.DiscountAmount
                    }).ToList();

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

        // === حدث تغيير الفلتر (Checkbox) ===
        private void chkShowVoided_Checked(object sender, RoutedEventArgs e)
        {
            _currentPage = 1;
            LoadOrders();
        }

        private void chkShowVoided_Unchecked(object sender, RoutedEventArgs e)
        {
            _currentPage = 1;
            LoadOrders();
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
                    // عند البحث برقم الفاتورة، نبحث في الكل (المحذوف والفعال)
                    // لأنك قد تبحث عن فاتورة قديمة للتأكد منها
                    var result = context.Orders
                        .Where(o => o.OrderNumber == orderNum) // أو Id حسب ما تستخدم
                        .ToList();

                    dgOrders.ItemsSource = result;
                    txtPageInfo.Text = result.Count > 0 ? "نتائج البحث" : "لا توجد نتائج";

                    // تنبيه إذا كانت الفاتورة ملغاة
                    if (result.Any(o => o.isDeleted))
                    {
                        ToastControl.Show("معلومات", "هذه الفاتورة محذوفة (ملفاة)", ToastControl.NotificationType.Info);
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

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Text = "";
            chkShowVoided.IsChecked = false; // إعادة الفلتر للوضع الطبيعي
            _currentPage = 1;
            LoadOrders();
        }

        private void TxtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) Search_Click(sender, e);
        }

        private void PrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1) { _currentPage--; LoadOrders(); }
        }

        private void NextPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage < _totalPages) { _currentPage++; LoadOrders(); }
        }

        private void ViewDetails_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                var detailsWindow = new OrderDetailsWindow(id);
                detailsWindow.ShowDialog();
            }
        }

        // === الإلغاء (Soft Delete) ===
        private void DeleteOrder_Click(object sender, RoutedEventArgs e)
        {
            // التحقق من الصلاحية (الأدمن فقط)
            if (CurrentUser.Role != (int)UserRole.Admin)
            {
                ToastControl.Show( "صلاحيات","عذراً، إلغاء الفواتير متاح للمدير فقط.", ToastControl.NotificationType.Warning);
                
                return;
            }

            if (sender is Button btn && btn.Tag is int id)
            {
                if (MessageBox.Show("هل أنت متأكد من إلغاء هذه الفاتورة؟\nسيتم نقلها للأرشيف ولن تحتسب في المبيعات.",
                    "تأكيد الإلغاء", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (var context = new VinceSweetsDbContext())
                        {
                            // 1. جلب الطلب مع تفاصيله
                            var order = context.Orders
                                .Include(o => o.OrderDetails)
                                .FirstOrDefault(o => o.Id == id);

                            if (order != null)
                            {
                                // التحقق: هل هي ملغاة بالفعل؟
                                if (order.isDeleted)
                                {
                                    ToastControl.Show("معلومات", "هذه الفاتورة محذوفة (ملفاة)", ToastControl.NotificationType.Info);
                                    return;
                                }

                                // 2. تنفيذ Soft Delete (تحديث الحالة فقط)
                                order.isDeleted = true;

                                // اختياري: إلغاء التفاصيل أيضاً لضمان التناسق
                                foreach (var detail in order.OrderDetails)
                                {
                                    detail.isDeleted = true;
                                }

                                // 3. الحفظ (هنا سيعمل Audit Log ويسجل أن الأدمن قام بـ SoftDelete)
                                context.SaveChanges();

                                ToastControl.Show("معلومات", "تم الغاء الفاتورة ونقلها الى الارشيف", ToastControl.NotificationType.Success);
                                LoadOrders(); // تحديث القائمة لإخفاء الفاتورة الملغاة
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error deleting order");
                        ToastControl.Show("معلومات", "فشل الغاء الفاتورة", ToastControl.NotificationType.Error);
                    }
                }
            }
        }
    }
}