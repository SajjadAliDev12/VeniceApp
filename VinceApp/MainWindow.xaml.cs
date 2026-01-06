using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
    // مودل مساعد لعرض التصنيف
    public class CategoryViewModel
    {
        public string Name { get; set; }
    }

    public partial class MainWindow : Window
    {
        private readonly int _currentOrderId;
        private readonly int? _currentTableId;
        private bool _isReadOnly = false;
        private string _currentInput = "0";
        private decimal _totalAmountToPay = 0;

        public ObservableCollection<CartItemViewModel> CartItems { get; } = new();
        public ObservableCollection<Product> DisplayedProducts { get; } = new();
        public ObservableCollection<CategoryViewModel> CategoriesList { get; } = new(); // قائمة التصنيفات الديناميكية

        private List<Product> AllProducts = new();

        public MainWindow(int orderId, int? tableId)
        {
            InitializeComponent();

            _currentOrderId = orderId;
            _currentTableId = tableId;

            Title.Text = _currentTableId.HasValue
                ? $"حلويات البندقية - طاولة رقم {_currentTableId}"
                : "حلويات البندقية - طلب سفري";

            lstCart.ItemsSource = CartItems;
            itemsControlProducts.ItemsSource = DisplayedProducts;
            icCategories.ItemsSource = CategoriesList; // ربط قائمة التصنيفات بالواجهة

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
                    MessageBox.Show(
                        $"تعذر تحميل البيانات.\n{ex.Message}",
                        "خطأ",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    Close();
                }
            };
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

        // دالة لتعطيل الواجهة
        private void EnableReadOnlyMode()
        {
            // تغيير العنوان
            Title.Text += " (للعرض فقط - مدفوع)";
            Title.Foreground = Brushes.Red;

            // تعطيل أزرار المنتجات
            itemsControlProducts.IsEnabled = false;

            // تعطيل قائمة التصنيفات
            icCategories.IsEnabled = false;

            // تعطيل أزرار التحكم في السلة (الزيادة والنقصان)
            lstCart.IsEnabled = false;

            if (btnPay != null)
            {
                btnPay.Content = "إغلاق"; // تغيير النص
                btnPay.Background = Brushes.Gray; // تغيير اللون لرمادي
                                                  // (اختياري) يمكنك تغيير الأيقونة أو أي شيء آخر
            }
        }
        // ===================== تحميل البيانات (منتجات + تصنيفات) =====================
        private async Task LoadDataFromDatabaseAsync()
        {
            await using var context = new VinceSweetsDbContext();

            // 1. تحميل المنتجات
            AllProducts = await context.Products.Include(p => p.Category).ToListAsync();

            // 2. تحميل التصنيفات من قاعدة البيانات
            var dbCategories = await context.Categories.OrderBy(c => c.Id).ToListAsync();

            // 3. تعبئة قائمة التصنيفات للواجهة
            CategoriesList.Clear();

            // إضافة زر "الكل" يدوياً
            CategoriesList.Add(new CategoryViewModel { Name = "الكل" });

            // إضافة باقي التصنيفات من الداتا بيس
            foreach (var cat in dbCategories)
            {
                CategoriesList.Add(new CategoryViewModel { Name = cat.Name });
            }

            // عرض الكل مبدئياً
            FilterProducts("الكل");
        }

        private async Task LoadExistingOrderDetailsAsync()
        {
            await using var context = new VinceSweetsDbContext();
            var details = await context.OrderDetails
                .Where(d => d.OrderId == _currentOrderId)
                .Select(d => new CartItemViewModel
                {
                    ProductId = d.ProductId,
                    Name = d.ProductName,
                    Price = d.Price,
                    Quantity = d.Quantity,
                    TotalPrice = d.Price * d.Quantity
                }).ToListAsync();

            CartItems.Clear();
            foreach (var i in details) CartItems.Add(i);
            CalculateTotal();
        }

        // ===================== الفلترة =====================
        private void FilterProducts(string categoryName)
        {
            DisplayedProducts.Clear();
            var filtered = (categoryName == "الكل" || string.IsNullOrWhiteSpace(categoryName))
                ? AllProducts
                : AllProducts.Where(p => p.Category?.Name == categoryName);

            foreach (var p in filtered) DisplayedProducts.Add(p);
        }

        private void Category_Click(object sender, RoutedEventArgs e)
        {
            // الآن نأخذ الاسم من الـ Tag المربوط بالداتا بيس
            if (sender is Button btn && btn.Tag is string categoryName)
            {
                FilterProducts(categoryName);
            }
        }

        // ===================== السلة (لم تتغير) =====================
        private async void Product_Click(object sender, RoutedEventArgs e)
        {
            if(_isReadOnly) return;
            if (sender is not Button b || b.DataContext is not Product product) return;

            try
            {
                await using var context = new VinceSweetsDbContext();
                // 1. البحث عن المنتج في السلة الحالية (في الذاكرة - للعرض)
                var item = CartItems.FirstOrDefault(i => i.ProductId == product.Id);

                // 2. البحث عن المنتج في قاعدة البيانات (للطلب الحالي)
                var dbDetail = await context.OrderDetails
                    .FirstOrDefaultAsync(d => d.OrderId == _currentOrderId && d.ProductId == product.Id);

                if (item != null)
                {
                    item.Quantity++;
                    item.TotalPrice = item.Price * item.Quantity;
                    if (dbDetail != null) dbDetail.Quantity = item.Quantity;
                }
                else
                {
                    var newItem = new CartItemViewModel
                    {
                        ProductId = product.Id,
                        Name = product.Name,
                        Price = product.Price,
                        Quantity = 1,
                        TotalPrice = product.Price
                    };
                    CartItems.Add(newItem);

                    context.OrderDetails.Add(new OrderDetail
                    {
                        OrderId = _currentOrderId,
                        ProductId = product.Id,
                        ProductName = product.Name,
                        Price = product.Price,
                        Quantity = 1
                    });
                }

                await UpdateOrderTotalAmountAsync(context);
                await context.SaveChangesAsync();
                CalculateTotal();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"فشل إضافة المنتج.\nالخطأ: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void IncreaseQty_Click(object sender, RoutedEventArgs e)
        {
            if(_isReadOnly) return;
            var btn = sender as Button;
            if (btn?.Tag == null) return;

            if (int.TryParse(btn.Tag.ToString(), out int productId))
            {
                var item = CartItems.FirstOrDefault(i => i.ProductId == productId);
                if (item != null)
                {
                    try
                    {
                        await using var context = new VinceSweetsDbContext();
                        item.Quantity++;
                        item.TotalPrice = item.Price * item.Quantity;

                        var dbDetail = await context.OrderDetails
                            .FirstOrDefaultAsync(d => d.OrderId == _currentOrderId && d.ProductId == item.ProductId);

                        if (dbDetail != null)
                        {
                            dbDetail.Quantity = item.Quantity;
                            await UpdateOrderTotalAmountAsync(context);
                            await context.SaveChangesAsync();
                        }
                    }
                    catch
                    {
                        item.Quantity--; // Rollback on error
                        MessageBox.Show("حدث خطأ أثناء تحديث الكمية", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    CalculateTotal();
                }
            }
        }

        private async void DecreaseQty_Click(object sender, RoutedEventArgs e)
        {
            if (_isReadOnly) return;
            var btn = sender as Button;
            if (btn?.Tag == null) return;

            if (int.TryParse(btn.Tag.ToString(), out int productId))
            {
                var item = CartItems.FirstOrDefault(i => i.ProductId == productId);
                if (item != null)
                {
                    try
                    {
                        await using var context = new VinceSweetsDbContext();
                        var dbDetail = await context.OrderDetails
                                .FirstOrDefaultAsync(d => d.OrderId == _currentOrderId && d.ProductId == item.ProductId);

                        item.Quantity--;

                        if (item.Quantity <= 0)
                        {
                            CartItems.Remove(item);
                            if (dbDetail != null) context.OrderDetails.Remove(dbDetail);
                        }
                        else
                        {
                            item.TotalPrice = item.Price * item.Quantity;
                            if (dbDetail != null) dbDetail.Quantity = item.Quantity;
                        }

                        await UpdateOrderTotalAmountAsync(context);
                        await context.SaveChangesAsync();
                    }
                    catch
                    {
                        MessageBox.Show("حدث خطأ أثناء تحديث الكمية", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    CalculateTotal();
                }
            }
        }

        private async Task UpdateOrderTotalAmountAsync(VinceSweetsDbContext context)
        {
            var order = await context.Orders.FindAsync(_currentOrderId);
            if (order == null) return;
            order.TotalAmount = await context.OrderDetails
                .Where(d => d.OrderId == _currentOrderId)
                .SumAsync(d => d.Price * d.Quantity);
        }

        private void CalculateTotal()
        {
            var total = CartItems.Sum(i => i.TotalPrice);
            txtTotal.Text = $"{total:N0} د.ع";
        }

        // ===================== باقي الأزرار (خروج ودفع) =====================
        private void PayButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isReadOnly)
            {
                Close(); // إغلاق النافذة فقط
                return;
            }
            if (!CartItems.Any()) return;
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
                MessageBox.Show("المبلغ غير كافٍ!", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            try
            {
                await using var context = new VinceSweetsDbContext();
                await using var tx = await context.Database.BeginTransactionAsync();
                var order = await context.Orders.FindAsync(_currentOrderId);
                if (order != null) order.OrderStatus = "Paid";
                if (_currentTableId.HasValue)
                {
                    var table = await context.RestaurantTables.FindAsync(_currentTableId.Value);
                    if (table != null) table.Status = 2;
                }
                await context.SaveChangesAsync();
                await tx.CommitAsync();
                MessageBox.Show("تم الدفع بنجاح.", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                PaymentOverlay.Visibility = Visibility.Collapsed;
                Close();
            }
            catch
            {
                MessageBox.Show("فشل إتمام الدفع.", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!CartItems.Any())
                {
                    await using var context = new VinceSweetsDbContext();
                    var order = await context.Orders.FindAsync(_currentOrderId);
                    if (order != null) context.Orders.Remove(order);
                    if (_currentTableId.HasValue)
                    {
                        var table = await context.RestaurantTables.FindAsync(_currentTableId.Value);
                        if (table != null) table.Status = 0;
                    }
                    await context.SaveChangesAsync();
                }
                Close();
            }
            catch
            {
                MessageBox.Show("تعذر الإغلاق.", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Numpad_Click(object sender, RoutedEventArgs e) { if (sender is Button btn) { _currentInput = _currentInput == "0" ? btn.Content.ToString() : _currentInput + btn.Content.ToString(); UpdatePaymentDisplay(); } }
        private void Numpad_Clear_Click(object sender, RoutedEventArgs e) { _currentInput = "0"; UpdatePaymentDisplay(); }
        private void Numpad_Backspace_Click(object sender, RoutedEventArgs e) { _currentInput = _currentInput.Length > 1 ? _currentInput[..^1] : "0"; UpdatePaymentDisplay(); }
        private void UpdatePaymentDisplay() { decimal.TryParse(_currentInput, out var paid); txtPaidAmount.Text = paid.ToString("N0"); var change = paid - _totalAmountToPay; txtChangeAmount.Text = change < 0 ? "غير كافٍ" : change.ToString("N0"); txtChangeAmount.Foreground = change < 0 ? Brushes.Red : Brushes.Green; }
        private void CloseOverlay_Click(object sender, RoutedEventArgs e) { PaymentOverlay.Visibility = Visibility.Collapsed; }
    }

    // ViewModel للعرض
    public class CartItemViewModel : INotifyPropertyChanged
    {
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