using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VinceApp.Data;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.Runtime.CompilerServices;


namespace VinceApp
{
    public class CategoryViewModel
    {
        public string Name { get; set; }
    }

    public partial class MainWindow : Window
    {
        private readonly int _currentOrderId;
        private readonly int? _currentTableId;

        // إدارة الحالة (State Management)
        private bool _isReadOnly = false;
        private bool _isDirty = false; // هل تم تعديل السلة دون حفظ؟
        private List<int> _deletedDetailsIds = new List<int>(); // لتتبع العناصر المحذوفة من طلب قديم

        private string _currentInput = "0";
        private decimal _totalAmountToPay = 0;

        public ObservableCollection<CartItemViewModel> CartItems { get; } = new();
        public ObservableCollection<Product> DisplayedProducts { get; } = new();
        public ObservableCollection<CategoryViewModel> CategoriesList { get; } = new();

        private List<Product> AllProducts = new();

        public MainWindow(int orderId, int? tableId, string? TableName)
        {
            InitializeComponent();
            _currentOrderId = orderId;
            _currentTableId = tableId;

            // تحديد العنوان
            if (_currentTableId.HasValue) Title.Text = $"طاولة رقم {_currentTableId} ({TableName})";
            else Title.Text = "طلب سفري";

            lstCart.ItemsSource = CartItems;
            itemsControlProducts.ItemsSource = DisplayedProducts;
            icCategories.ItemsSource = CategoriesList;

            Loaded += async (_, __) =>
            {
                try
                {
                    await LoadDataFromDatabaseAsync();
                    await LoadExistingOrderDetailsAsync();
                    await CheckIfReadOnly();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ في التحميل: {ex.Message}");
                    Close();
                }
            };
        }

        // ===================== التحميل =====================
        private async Task LoadDataFromDatabaseAsync()
        {
            await using var context = new VinceSweetsDbContext();
            AllProducts = await context.Products.Include(p => p.Category).ToListAsync();

            var dbCategories = await context.Categories.OrderBy(c => c.Id).ToListAsync();
            CategoriesList.Clear();
            CategoriesList.Add(new CategoryViewModel { Name = "الكل" });
            foreach (var cat in dbCategories) CategoriesList.Add(new CategoryViewModel { Name = cat.Name });

            FilterProducts("الكل");
        }

        private async Task LoadExistingOrderDetailsAsync()
        {
            await using var context = new VinceSweetsDbContext();
            var details = await context.OrderDetails
                .Where(d => d.OrderId == _currentOrderId)
                .Select(d => new CartItemViewModel
                {
                    OrderDetailId = d.Id, // نحتفظ بالـ ID الأصلي
                    ProductId = d.ProductId,
                    Name = d.ProductName,
                    Price = d.Price,
                    Quantity = d.Quantity,
                    TotalPrice = d.Price * d.Quantity
                }).ToListAsync();

            CartItems.Clear();
            foreach (var i in details) CartItems.Add(i);
            CalculateTotal();

            _isDirty = false; // البيانات متطابقة مع الداتا بيس
        }

        private async Task CheckIfReadOnly()
        {
            using (var context = new VinceSweetsDbContext())
            {
                var order = await context.Orders.FindAsync(_currentOrderId);
                if (order != null && (order.OrderStatus == "Paid" || order.OrderStatus == "Completed"))
                {
                    _isReadOnly = true;
                    EnableReadOnlyMode();
                }
            }
        }

        private void EnableReadOnlyMode()
        {
            Title.Text += " (للعرض فقط - مدفوع)";
            Title.Foreground = Brushes.Red;
            itemsControlProducts.IsEnabled = false;
            icCategories.IsEnabled = false;
            lstCart.IsEnabled = false;

            // إخفاء زر الحفظ وتحويل زر الدفع لإغلاق
            btnConfirm.Visibility = Visibility.Collapsed;
            if (btnPay != null)
            {
                btnPay.Content = "إغلاق";
                btnPay.Background = Brushes.Gray;
            }
        }

        // ===================== إدارة السلة (الذاكرة فقط) =====================
        private void Product_Click(object sender, RoutedEventArgs e)
        {
            if (_isReadOnly) return;
            if (sender is not Button b || b.DataContext is not Product product) return;
            if (product.IsAvailable == false) 
            {
                MessageBox.Show("هذا العنصر غير متوفر حالياً","Error",MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            ;
            var item = CartItems.FirstOrDefault(i => i.ProductId == product.Id);
            if (item != null)
            {
                item.Quantity++;
                item.TotalPrice = item.Price * item.Quantity;
            }
            else
            {
                CartItems.Add(new CartItemViewModel
                {
                    OrderDetailId = 0, // 0 يعني جديد لم يحفظ بعد
                    ProductId = product.Id,
                    Name = product.Name,
                    Price = product.Price,
                    Quantity = 1,
                    TotalPrice = product.Price
                });
                // تمرير السكرول للأسفل
                if (VisualTreeHelper.GetChildrenCount(lstCart) > 0)
                {
                    Border border = (Border)VisualTreeHelper.GetChild(lstCart, 0);
                    ScrollViewer scrollViewer = (ScrollViewer)VisualTreeHelper.GetChild(border, 0);
                    scrollViewer.ScrollToBottom();
                }
            }

            CalculateTotal();
            _isDirty = true; // تم التعديل
        }

        private void IncreaseQty_Click(object sender, RoutedEventArgs e)
        {
            if (_isReadOnly) return;
            if (sender is Button btn && btn.Tag is int prodId)
            {
                var item = CartItems.FirstOrDefault(i => i.ProductId == prodId);
                if (item != null)
                {
                    item.Quantity++;
                    item.TotalPrice = item.Price * item.Quantity;
                    CalculateTotal();
                    _isDirty = true;
                }
            }
        }

        private void DecreaseQty_Click(object sender, RoutedEventArgs e)
        {
            if (_isReadOnly) return;
            if (sender is Button btn && btn.Tag is int prodId)
            {
                var item = CartItems.FirstOrDefault(i => i.ProductId == prodId);
                if (item != null)
                {
                    item.Quantity--;
                    if (item.Quantity <= 0)
                    {
                        // إذا كان عنصراً قديماً (له ID في الداتا بيس)، نضيفه لقائمة الحذف
                        if (item.OrderDetailId > 0)
                        {
                            _deletedDetailsIds.Add(item.OrderDetailId);
                        }
                        CartItems.Remove(item);
                    }
                    else
                    {
                        item.TotalPrice = item.Price * item.Quantity;
                    }
                    CalculateTotal();
                    _isDirty = true;
                }
            }
        }

        // ===================== زر الحفظ (تأكيد الطلب) =====================
        private async void ConfirmOrder_Click(object sender, RoutedEventArgs e)
        {
            if (_isReadOnly) return;
            if (!CartItems.Any() && !_deletedDetailsIds.Any()) return; // لا يوجد شيء للحفظ

            await SaveChangesToDatabaseAsync();
            MessageBox.Show("تم حفظ الطلب وإرساله للمطبخ.", "تأكيد", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // دالة مركزية للحفظ في قاعدة البيانات (تستخدم عند التأكيد وعند الدفع)
        private async Task SaveChangesToDatabaseAsync()
        {
            using (var context = new VinceSweetsDbContext())
            using (var tx = await context.Database.BeginTransactionAsync())
            {
                // 1. حذف العناصر التي أزالها المستخدم
                if (_deletedDetailsIds.Any())
                {
                    var toDelete = await context.OrderDetails
                        .Where(d => _deletedDetailsIds.Contains(d.Id))
                        .ToListAsync();
                    context.OrderDetails.RemoveRange(toDelete);
                }

                // 2. إضافة أو تحديث العناصر الموجودة في السلة
                foreach (var item in CartItems)
                {
                    if (item.OrderDetailId == 0)
                    {
                        // عنصر جديد -> إضافة
                        var newDetail = new OrderDetail
                        {
                            OrderId = _currentOrderId,
                            ProductId = item.ProductId,
                            ProductName = item.Name,
                            Price = item.Price,
                            Quantity = item.Quantity,
                            // هنا يمكن إضافة IsSentToKitchen = true مستقبلاً
                        };
                        context.OrderDetails.Add(newDetail);
                    }
                    else
                    {
                        // عنصر قديم -> تحديث الكمية فقط
                        var existingDetail = await context.OrderDetails.FindAsync(item.OrderDetailId);
                        if (existingDetail != null)
                        {
                            existingDetail.Quantity = item.Quantity;
                        }
                    }
                }

                // 3. تحديث إجمالي الطلب
                var currentOrderTotal = CartItems.Sum(x => x.TotalPrice);
                var order = await context.Orders.FindAsync(_currentOrderId);
                if (order != null)
                {
                    order.TotalAmount = currentOrderTotal;
                }
                if (_currentTableId.HasValue)
                {
                    var table = await context.RestaurantTables.FindAsync(_currentTableId.Value);
                    // إذا كانت الطاولة فارغة، نحولها إلى مشغولة (1) لأننا حفظنا فيها طلبات الآن
                    if (table != null && table.Status == 0)
                    {
                        table.Status = 1; // Busy
                    }
                }

                await context.SaveChangesAsync();
                await tx.CommitAsync();

                // 4. إعادة تعيين الحالة
                _isDirty = false;
                _deletedDetailsIds.Clear();

                // إعادة تحميل البيانات لضمان الحصول على الـ IDs الجديدة للعناصر المضافة
                await LoadExistingOrderDetailsAsync();
            }
        }

        // ===================== زر الدفع (حفظ + دفع) =====================
        private async void PayButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isReadOnly)
            {
                Close();
                return;
            }

            if (!CartItems.Any()) return;

            // 1. حفظ التغييرات أولاً (في حال عدل ولم يضغط حفظ)
            if (_isDirty)
            {
                await SaveChangesToDatabaseAsync();
            }

            // 2. إظهار نافذة الدفع
            _totalAmountToPay = CartItems.Sum(i => i.TotalPrice);
            txtOverlayTotal.Text = _totalAmountToPay.ToString("N0");
            _currentInput = "0";
            UpdatePaymentDisplay();
            PaymentOverlay.Visibility = Visibility.Visible;
        }

        private async void PrintReceipt_Click(object sender, RoutedEventArgs e)
        {
            if (!decimal.TryParse(_currentInput, out var paid) || paid < _totalAmountToPay)
            {
                MessageBox.Show("المبلغ غير كافٍ!", "تنبيه");
                return;
            }

            try
            {
                using (var context = new VinceSweetsDbContext())
                {
                    var order = await context.Orders.FindAsync(_currentOrderId);
                    if (order != null) order.OrderStatus = "Paid";

                    if (_currentTableId.HasValue)
                    {
                        var table = await context.RestaurantTables.FindAsync(_currentTableId.Value);
                        if (table != null) table.Status = 2; // Paid
                    }
                    await context.SaveChangesAsync();
                }

                MessageBox.Show("تم الدفع بنجاح.");
                PaymentOverlay.Visibility = Visibility.Collapsed;
                Close();
            }
            catch { MessageBox.Show("فشل الدفع"); }
        }

        // ===================== الخروج (الحماية) =====================
        private async void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isReadOnly) { Close(); return; }

            // فحص التعديلات غير المحفوظة
            if (_isDirty)
            {
                var result = MessageBox.Show("توجد تعديلات غير محفوظة، هل تود الخروج وإهمالها؟", "تنبيه", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.No) return;
            }

            try
            {
                // تنظيف الطلبات الفارغة التي لم تحفظ
                using (var context = new VinceSweetsDbContext())
                {
                    var currentOrder = await context.Orders.FindAsync(_currentOrderId);

                    // إذا كان الطلب فارغاً تماماً (لم يتم تأكيد أي صنف فيه)
                    // والتفاصيل في الداتا بيس فارغة
                    bool hasDetails = await context.OrderDetails.AnyAsync(d => d.OrderId == _currentOrderId);

                    if (currentOrder != null && !hasDetails)
                    {
                        context.Orders.Remove(currentOrder);
                        // لا نحتاج لتغيير حالة الطاولة لأنها أصلاً 0 (خضراء) حسب التعديل الجديد
                        await context.SaveChangesAsync();
                    }
                }
            }
            catch { MessageBox.Show("حدث خطأ في البرنامج!", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error); }

            Close();
        }

        // دوال المساعدة للواجهة
        private void FilterProducts(string categoryName)
        {
            DisplayedProducts.Clear();
            var filtered = (categoryName == "الكل" || string.IsNullOrWhiteSpace(categoryName)) ? AllProducts : AllProducts.Where(p => p.Category?.Name == categoryName);
            foreach (var p in filtered) DisplayedProducts.Add(p);
        }
        private void Category_Click(object sender, RoutedEventArgs e) { if (sender is Button btn && btn.Tag is string c) FilterProducts(c); }
        private void CalculateTotal() { txtTotal.Text = $"{CartItems.Sum(i => i.TotalPrice):N0} د.ع"; }
        private void CloseOverlay_Click(object sender, RoutedEventArgs e) => PaymentOverlay.Visibility = Visibility.Collapsed;
        private void Numpad_Click(object sender, RoutedEventArgs e) { if (sender is Button b) { _currentInput = _currentInput == "0" ? b.Content.ToString() : _currentInput + b.Content.ToString(); UpdatePaymentDisplay(); } }
        private void Numpad_Clear_Click(object sender, RoutedEventArgs e) { _currentInput = "0"; UpdatePaymentDisplay(); }
        private void Numpad_Backspace_Click(object sender, RoutedEventArgs e) { _currentInput = _currentInput.Length > 1 ? _currentInput[..^1] : "0"; UpdatePaymentDisplay(); }
        private void UpdatePaymentDisplay() { decimal.TryParse(_currentInput, out var paid); txtPaidAmount.Text = paid.ToString("N0"); var change = paid - _totalAmountToPay; txtChangeAmount.Text = change < 0 ? "غير كافٍ" : change.ToString("N0"); txtChangeAmount.Foreground = change < 0 ? Brushes.Red : Brushes.Green; }

        // زر تصدير المنيو (كما هو)
        
    }

    public class CartItemViewModel : INotifyPropertyChanged
    {
        public int OrderDetailId { get; set; } // مهم جداً: 0 = جديد، >0 = موجود في الداتا بيس
        public int ProductId { get; set; }
        public string Name { get; set; } = "";
        public decimal Price { get; set; }

        private int _quantity;
        public int Quantity { get => _quantity; set { if (_quantity != value) { _quantity = value; OnPropertyChanged(); } } }

        private decimal _totalPrice;
        public decimal TotalPrice { get => _totalPrice; set { if (_totalPrice != value) { _totalPrice = value; OnPropertyChanged(); } } }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}