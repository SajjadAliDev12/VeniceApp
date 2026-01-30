using Serilog;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using VinceApp.Data;
using VinceApp.Data.Models;
using VinceApp.Services;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace VinceApp
{
    public class CategoryViewModel
    {
        public string Name { get; set; }
    }

    public partial class MainWindow : Window
    {
        private readonly int _currentOrderId;
        private bool _initialized = false;
        private readonly int? _currentTableId;
        private int? _parentOrderId = null;

        // إدارة الحالة (State Management)
        private bool _isReadOnly = false;
        private bool _isDirty = false;
        private List<int> _deletedDetailsIds = new List<int>();
        public bool wasPaid { get; private set; } = false;

        private string _currentInput = "0";
        private decimal _totalAmountToPay = 0;
        private decimal _discountAmount = 0;
        private decimal _subTotalAmount = 0;

        public ObservableCollection<CartItemViewModel> CartItems { get; } = new();
        public ObservableCollection<Product> DisplayedProducts { get; } = new();
        public ObservableCollection<CategoryViewModel> CategoriesList { get; } = new();

        private List<Product> AllProducts = new();

        public MainWindow(int orderId, int? tableId, string? TableName, int? _ParentOrderId = null)
        {
            InitializeComponent();

            if (Application.Current.Properties["DisableSounds"] as bool? != true)
            {
                Task.Run(() => { try { string soundFile = FIlePathFinder.GetPath("Windows Navigation Start.wav"); if (System.IO.File.Exists(soundFile)) { using (var player = new System.Media.SoundPlayer(soundFile)) { player.PlaySync(); } } } catch { } });
            }

            _currentOrderId = orderId;
            _currentTableId = tableId;
            _parentOrderId = _ParentOrderId;

            // ✅ تعديل 1: تعيين العنوان فوراً برقم الطاولة وحذف نص (جاري التحميل)
            string baseTitle = "";
            if (_currentTableId.HasValue)
            {
                // نعتمد على الاسم الممرر مبدئياً ليظهر الرقم فوراً
                baseTitle = TableName ?? "طاولة";
            }
            else
            {
                baseTitle = "طلب سفري";
            }
            Title.Text = baseTitle;

            ShowLoading(true, "جاري تحميل الطلبات......");
            lstCart.ItemsSource = CartItems;
            itemsControlProducts.ItemsSource = DisplayedProducts;
            icCategories.ItemsSource = CategoriesList;

            ContentRendered += async (_, __) =>
            {
                if (_initialized) return;
                _initialized = true;

                try
                {
                    Mouse.OverrideCursor = Cursors.Wait;

                    var loadedData = await Task.Run(async () =>
                    {
                        // 1. نجلب بيانات الطلب
                        var orderData = await FetchOrderInfoAsync(_currentOrderId);

                        // 2. معالجة الملحق (الطلب الأب)
                        int? targetParentId = _parentOrderId;
                        if (targetParentId == null && orderData != null)
                        {
                            targetParentId = orderData.ParentOrderId;
                        }

                        int parentOrderNumber = 0;
                        if (targetParentId.HasValue)
                        {
                            using (var ctx = new VinceSweetsDbContext())
                            {
                                parentOrderNumber = await ctx.Orders
                                    .Where(o => o.Id == targetParentId.Value)
                                    .Select(o => o.OrderNumber)
                                    .FirstOrDefaultAsync();
                            }
                        }

                        // ✅ تعديل 2: جلب رقم الطاولة الفعلي من قاعدة البيانات لضمان صحة العنوان
                        string realTableNameFromDb = null;
                        if (_currentTableId.HasValue)
                        {
                            using (var ctx = new VinceSweetsDbContext())
                            {
                                var tbl = await ctx.RestaurantTables
                                    .Where(t => t.Id == _currentTableId.Value)
                                    .Select(t => t.TableNumber)
                                    .FirstOrDefaultAsync();

                                if (tbl > 0) realTableNameFromDb = $"طاولة {tbl}";
                            }
                        }

                        // 3. جلب المنتجات والتفاصيل
                        var t1 = FetchProductsAndCategoriesAsync();
                        var t2 = FetchExistingOrderDetailsAsync(_currentOrderId);

                        await Task.WhenAll(t1, t2);

                        return (
                            ProductsData: t1.Result,
                            CartData: t2.Result,
                            OrderData: orderData,
                            ParentOrderNum: parentOrderNumber,
                            RealTableName: realTableNameFromDb // الاسم المؤكد من القاعدة
                        );
                    });

                    // --- تحديث الواجهة ---

                    // 1. المنتجات
                    AllProducts = loadedData.ProductsData.products;
                    CategoriesList.Clear();
                    CategoriesList.Add(new CategoryViewModel { Name = "الكل" });
                    foreach (var cat in loadedData.ProductsData.categories) CategoriesList.Add(new CategoryViewModel { Name = cat.Name });
                    FilterProducts("الكل");

                    // 2. السلة
                    CartItems.Clear();
                    foreach (var item in loadedData.CartData) CartItems.Add(item);
                    CalculateTotal();
                    _isDirty = false;

                    // ✅ تعديل 3: تحديث العنوان النهائي (اسم الطاولة المؤكد + الملحق إن وجد)
                    string finalBaseTitle = baseTitle;

                    // اذا جلبنا الاسم من القاعدة نستخدمه لأنه أدق
                    if (!string.IsNullOrEmpty(loadedData.RealTableName))
                    {
                        finalBaseTitle = loadedData.RealTableName;
                    }

                    if (loadedData.ParentOrderNum > 0)
                    {
                        Title.Text = $"{finalBaseTitle} - ملحق لطلب رقم {loadedData.ParentOrderNum}";
                    }
                    else
                    {
                        Title.Text = finalBaseTitle;
                    }

                    // 4. وضع القراءة فقط
                    var order = loadedData.OrderData;
                    if (order != null && (order.isPaid || order.isServed || order.isReady))
                    {
                        _isReadOnly = true;
                        EnableReadOnlyMode(order);
                    }
                }
                catch (Exception ex)
                {
                    ToastControl.Show("error", "خطأ في التحميل", ToastControl.NotificationType.Error);
                    Log.Error(ex, "failed to obtain data from database");
                    Close();
                }
                finally
                {
                    Mouse.OverrideCursor = null;
                    ShowLoading(false);
                }
            };
        }

        // ... (بقية الدوال المساعدة كما هي تماماً بدون تغيير) ...

        // دالة تجلب المنتجات والأقسام
        private async Task<(List<Product> products, List<Category> categories)> FetchProductsAndCategoriesAsync()
        {
            await using var context = new VinceSweetsDbContext();
            var products = await context.Products.Include(p => p.Category).ToListAsync();
            var categories = await context.Categories.OrderBy(c => c.Id).ToListAsync();
            return (products, categories);
        }

        // دالة تجلب تفاصيل الطلب
        private async Task<List<CartItemViewModel>> FetchExistingOrderDetailsAsync(int orderId)
        {
            await using var context = new VinceSweetsDbContext();
            return await context.OrderDetails
                .Where(d => d.OrderId == orderId && !d.isDeleted)
                .Select(d => new CartItemViewModel
                {
                    OrderDetailId = d.Id,
                    ProductId = d.ProductId,
                    Name = d.ProductName,
                    Price = d.Price,
                    Quantity = d.Quantity,
                    TotalPrice = d.Price * d.Quantity
                }).ToListAsync();
        }

        // دالة تجلب معلومات الطلب
        private async Task<Order?> FetchOrderInfoAsync(int orderId)
        {
            await using var context = new VinceSweetsDbContext();
            return await context.Orders.FindAsync(orderId);
        }

        private void EnableReadOnlyMode(Order order = null)
        {
            itemsControlProducts.IsEnabled = false;
            icCategories.IsEnabled = false;
            btnConfirm.Visibility = Visibility.Collapsed;
            if (order != null)
            {
                if (order.isPaid || order.isServed)
                {
                    Title.Text += " (للعرض فقط)";
                    if (btnPay != null) { btnPay.Content = "إغلاق"; btnPay.Background = Brushes.Gray; }
                }
                else if (order.isReady) { Title.Text += " (جاهز - لا يمكن التعديل)"; }
            }
        }

        // ... (Product_Click, IncreaseQty_Click, DecreaseQty_Click, ConfirmOrder_Click, SaveChanges, PayButton, Print, ExitButton, Helpers...)
        // (انسخ بقية الدوال كما كانت في الكود السابق، التغيير فقط كان في الكونستركتور والـ ContentRendered أعلاه)

        private void Product_Click(object sender, RoutedEventArgs e)
        {
            if (_isReadOnly) return;
            if (sender is not Button b || b.DataContext is not Product product) return;
            if (product.IsAvailable == false) { ToastControl.Show("المنتج غير متوفر", "لا يمكن اضافة المنتج الى الطلب حالياً", ToastControl.NotificationType.Success); return; }
            var item = CartItems.FirstOrDefault(i => i.ProductId == product.Id);
            if (item != null) { item.Quantity++; item.TotalPrice = item.Price * item.Quantity; }
            else
            {
                CartItems.Add(new CartItemViewModel { OrderDetailId = 0, ProductId = product.Id, Name = product.Name, Price = product.Price, Quantity = 1, TotalPrice = product.Price });
                if (VisualTreeHelper.GetChildrenCount(lstCart) > 0)
                {
                    Border border = (Border)VisualTreeHelper.GetChild(lstCart, 0);
                    ScrollViewer scrollViewer = (ScrollViewer)VisualTreeHelper.GetChild(border, 0);
                    scrollViewer.ScrollToBottom();
                }
            }
            CalculateTotal(); _isDirty = true;
        }

        private void IncreaseQty_Click(object sender, RoutedEventArgs e) { if (_isReadOnly) return; if (sender is Button btn && btn.Tag is int prodId) { var item = CartItems.FirstOrDefault(i => i.ProductId == prodId); if (item != null) { item.Quantity++; item.TotalPrice = item.Price * item.Quantity; CalculateTotal(); _isDirty = true; } } }

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
                        if (item.OrderDetailId > 0 && !_deletedDetailsIds.Contains(item.OrderDetailId)) _deletedDetailsIds.Add(item.OrderDetailId);
                        CartItems.Remove(item);
                    }
                    else { item.TotalPrice = item.Price * item.Quantity; }
                    CalculateTotal(); _isDirty = true;
                }
            }
        }

        private async void ConfirmOrder_Click(object sender, RoutedEventArgs e)
        {
            if (_isReadOnly) return;
            if (!CartItems.Any() && !_deletedDetailsIds.Any()) return;
            await SaveChangesToDatabaseAsync();
            using (var context = new VinceSweetsDbContext())
            {
                var order = await context.Orders.FindAsync(_currentOrderId);
                if (order != null) { order.isSentToKitchen = true; await context.SaveChangesAsync(); }
                ToastControl.Show("تم الحفظ", " تم حفظ الطلب بنجاح وارساله الى المطبخ", ToastControl.NotificationType.Success);
                var details = context.OrderDetails.Where(d => d.OrderId == _currentOrderId).Select(d => d.Product.IsKitchenItem).ToList();
                if (details.Any()) { foreach (var item in details) { if (item != false) return; } order.isReady = true; await context.SaveChangesAsync(); }
            }
        }

        private async Task SaveChangesToDatabaseAsync()
        {
            using (var context = new VinceSweetsDbContext()) using (var tx = await context.Database.BeginTransactionAsync())
            {
                try
                {
                    bool hasActiveDetails = CartItems.Any();
                    if (_deletedDetailsIds.Any()) { var toDelete = await context.OrderDetails.Where(d => _deletedDetailsIds.Contains(d.Id)).ToListAsync(); foreach (var item in toDelete) item.isDeleted = true; }
                    foreach (var item in CartItems)
                    {
                        if (item.OrderDetailId == 0) { context.OrderDetails.Add(new OrderDetail { OrderId = _currentOrderId, ProductId = item.ProductId, ProductName = item.Name, Price = item.Price, Quantity = item.Quantity, isDeleted = false }); }
                        else { var existing = await context.OrderDetails.FindAsync(item.OrderDetailId); if (existing != null) { existing.Quantity = item.Quantity; existing.isDeleted = false; } }
                    }
                    var order = await context.Orders.FindAsync(_currentOrderId);
                    if (order != null)
                    {
                        if (_parentOrderId != null) order.ParentOrderId = _parentOrderId;
                        order.TotalAmount = CartItems.Sum(x => x.TotalPrice);
                        if (order.TableId.HasValue) { var table = await context.RestaurantTables.FindAsync(order.TableId.Value); if (table != null) table.Status = hasActiveDetails ? 1 : 0; }
                        if (!hasActiveDetails) { var all = await context.OrderDetails.Where(d => d.OrderId == _currentOrderId && !d.isDeleted).ToListAsync(); foreach (var d in all) d.isDeleted = true; order.isDeleted = true; order.TotalAmount = 0; }
                        else { order.isDeleted = false; }
                    }
                    await context.SaveChangesAsync(); await tx.CommitAsync(); _isDirty = false; _deletedDetailsIds.Clear();
                    var refreshedDetails = await FetchExistingOrderDetailsAsync(_currentOrderId); CartItems.Clear(); foreach (var item in refreshedDetails) CartItems.Add(item);
                }
                catch { await tx.RollbackAsync(); throw; }
            }
        }

        private async void PayButton_Click(object sender, RoutedEventArgs e)
        {
            if (btnPay.Content.ToString() == "إغلاق") { Close(); return; }
            if (!CartItems.Any()) return;
            _subTotalAmount = CartItems.Sum(i => i.TotalPrice); _discountAmount = 0; _currentInput = "0"; UpdatePaymentCalculations(); PaymentOverlay.Visibility = Visibility.Visible;
        }

        private async void PrintReceipt_Click(object sender, RoutedEventArgs e)
        {
            decimal.TryParse(_currentInput, out var paid);
            if (paid < _totalAmountToPay) { ToastControl.Show("خطأ", "المبلغ غير كافي", ToastControl.NotificationType.Error); return; }
            if (_discountAmount > (_totalAmountToPay * (decimal)0.15) && CurrentUser.Role == (int)UserRole.Cashier) { ToastControl.Show("تنبيه", "مبلغ الخصم كبير!", ToastControl.NotificationType.Warning); return; }
            try
            {
                await SaveChangesToDatabaseAsync();
                using (var context = new VinceSweetsDbContext())
                {
                    var order = await context.Orders.FindAsync(_currentOrderId);
                    if (order != null) { order.isPaid = true; wasPaid = true; order.isSentToKitchen = true; order.DiscountAmount = _discountAmount; }
                    if (_currentTableId.HasValue) { var table = await context.RestaurantTables.FindAsync(_currentTableId.Value); if (table != null) table.Status = 2; }
                    await context.SaveChangesAsync();
                }
                TicketPrinter Pr = new TicketPrinter(); Pr.Print(_currentOrderId, false);
                ToastControl.Show("تم الدفع", "تم الدفع بنجاح", ToastControl.NotificationType.Success); PaymentOverlay.Visibility = Visibility.Collapsed; this.DialogResult = wasPaid; Close();
            }
            catch (Exception ex) { Log.Error("Error in print MainWindow"); }
        }

        private async void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isReadOnly) { Close(); return; }
            if (_isDirty) { if (! await MyConfirmDialog.ShowAsync("تأكيد الأغلاق","هناك تغييرات لم تحفظ في الطلب هل انت متأكد من الخروج واهمالها؟")) return; }
            try
            {
                using (var context = new VinceSweetsDbContext())
                {
                    var currentOrder = await context.Orders.FindAsync(_currentOrderId);
                    bool hasDetails = await context.OrderDetails.AnyAsync(d => d.OrderId == _currentOrderId && !d.isDeleted);
                    if (currentOrder != null && !hasDetails)
                    {
                        if (currentOrder.TableId.HasValue)
                        {
                            var table = await context.RestaurantTables.FindAsync(currentOrder.TableId.Value);
                            if (table != null)
                            {
                                bool hasOtherPaidOrders = await context.Orders.AnyAsync(o => o.TableId == table.Id && o.Id != currentOrder.Id && o.isPaid && !o.isDone && !o.isDeleted);
                                table.Status = hasOtherPaidOrders ? 2 : 0;
                            }
                        }
                        context.Orders.Remove(currentOrder); await context.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex) { ToastControl.Show("خطأ", "حدث خطأ في البرنامج!", ToastControl.NotificationType.Error); Log.Error(ex, "Table window fault"); }
            Close();
        }

        private void FilterProducts(string categoryName) { DisplayedProducts.Clear(); var filtered = (categoryName == "الكل" || string.IsNullOrWhiteSpace(categoryName)) ? AllProducts : AllProducts.Where(p => p.Category?.Name == categoryName); foreach (var p in filtered) DisplayedProducts.Add(p); }
        private void ShowLoading(bool show, string? hint = null) { LoadingOverlay.Visibility = show ? Visibility.Visible : Visibility.Collapsed; if (!string.IsNullOrWhiteSpace(hint)) txtLoadingHint.Text = hint; }
        private void Category_Click(object sender, RoutedEventArgs e) { if (sender is Button btn && btn.Tag is string c) FilterProducts(c); }
        private void CalculateTotal() { txtTotal.Text = $"{CartItems.Sum(i => i.TotalPrice):N0} د.ع"; }
        private void CloseOverlay_Click(object sender, RoutedEventArgs e) => PaymentOverlay.Visibility = Visibility.Collapsed;
        private void Numpad_Click(object sender, RoutedEventArgs e) { if (sender is Button b) { _currentInput = _currentInput == "0" ? b.Content.ToString() : _currentInput + b.Content.ToString(); UpdatePaymentCalculations(); } }
        private void Numpad_Clear_Click(object sender, RoutedEventArgs e) { _currentInput = "0"; UpdatePaymentCalculations(); }
        private void Numpad_Backspace_Click(object sender, RoutedEventArgs e) { _currentInput = _currentInput.Length > 1 ? _currentInput[..^1] : "0"; UpdatePaymentCalculations(); }
        private void UpdatePaymentCalculations() { _totalAmountToPay = _subTotalAmount - _discountAmount; if (_totalAmountToPay < 0) _totalAmountToPay = 0; txtSubTotal.Text = _subTotalAmount.ToString("N0"); txtDiscountDisplay.Text = _discountAmount.ToString("N0"); txtOverlayTotal.Text = _totalAmountToPay.ToString("N0"); decimal.TryParse(_currentInput, out var paid); txtPaidAmount.Text = paid.ToString("N0"); var change = paid - _totalAmountToPay; txtChangeAmount.Text = change < 0 ? "غير كافٍ" : change.ToString("N0"); txtChangeAmount.Foreground = change < 0 ? Brushes.Red : Brushes.Green; }
        private void BtnEditDiscount_Click(object sender, RoutedEventArgs e) { var touchWindow = new DiscountInputWindow(_subTotalAmount); this.Opacity = 0.5; if (touchWindow.ShowDialog() == true) { _discountAmount = touchWindow.DiscountValue; UpdatePaymentCalculations(); } this.Opacity = 1; }
    }

    public class CartItemViewModel : INotifyPropertyChanged
    {
        public int OrderDetailId { get; set; }
        public int ProductId { get; set; }
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
        private int _quantity; public int Quantity { get => _quantity; set { if (_quantity != value) { _quantity = value; OnPropertyChanged(); } } }
        private decimal _totalPrice; public decimal TotalPrice { get => _totalPrice; set { if (_totalPrice != value) { _totalPrice = value; OnPropertyChanged(); } } }
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}