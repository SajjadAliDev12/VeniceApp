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

        private bool _isDirty = false; // هل تم تعديل السلة دون حفظ؟
        
        private List<int> _deletedDetailsIds = new List<int>(); // لتتبع العناصر المحذوفة من طلب قديم


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
                string soundFile = FIlePathFinder.GetPath("Windows Navigation Start.wav");
                if (System.IO.File.Exists(soundFile))
                {
                    using (var player = new System.Media.SoundPlayer(soundFile))
                    {
                        player.Play();
                    }
                }
            }
                _currentOrderId = orderId;

            _currentTableId = tableId;

            _parentOrderId = _ParentOrderId;

            // تحديد العنوان

            if (_currentTableId.HasValue) Title = $"طاولة رقم {_currentTableId} ({TableName})";

            else Title = "طلب سفري";

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

                    await Task.WhenAll(LoadDataFromDatabaseAsync(), LoadExistingOrderDetailsAsync(), CheckIfReadOnly());

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
                .Where(d => d.OrderId == _currentOrderId && !d.isDeleted)


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

                if (order != null && (order.isPaid || order.isServed || order.isReady))

                {

                    _isReadOnly = true;

                    EnableReadOnlyMode(order);

                }

            }

        }



        // عدل تعريف الدالة لتستقبل كائن الطلب

        private void EnableReadOnlyMode(Order order = null)

        {

            // قفل أدوات التعديل دائماً

            itemsControlProducts.IsEnabled = false;

            icCategories.IsEnabled = false;

            //lstCart.IsEnabled = true;

            

            // إخفاء زر الحفظ لأننا لا نستطيع تعديل المحتوى

            btnConfirm.Visibility = Visibility.Collapsed;



            if (order != null)

            {

                // الحالة 1: الطلب انتهى مالياً (مدفوع أو مسلم)

                if (order.isPaid || order.isServed)

                {

                    Title += " (للعرض فقط)";

                    



                    if (btnPay != null)

                    {

                        btnPay.Content = "إغلاق";

                        btnPay.Background = Brushes.Gray;

                    }

                }

                // الحالة 2: الطلب جاهز من المطبخ لكن لم يدفع (القفل التشغيلي)

                else if (order.isReady)

                {

                    Title += " (جاهز - لا يمكن التعديل)";

                    

                }

            }

        }



        // ===================== إدارة السلة (الذاكرة فقط) =====================

        private void Product_Click(object sender, RoutedEventArgs e)

        {

            if (_isReadOnly) return;

            if (sender is not Button b || b.DataContext is not Product product) return;

            if (product.IsAvailable == false)

            {

                ToastControl.Show("المنتج غير متوفر", "لا يمكن اضافة المنتج الى الطلب حالياً", ToastControl.NotificationType.Success);


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

                            if (item.OrderDetailId > 0 && !_deletedDetailsIds.Contains(item.OrderDetailId))
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



        private async void ConfirmOrder_Click(object sender, RoutedEventArgs e)

        {

            if (_isReadOnly) return;

            if (!CartItems.Any() && !_deletedDetailsIds.Any()) return;



            // 1. حفظ التفاصيل

            await SaveChangesToDatabaseAsync();
            using (var context = new VinceSweetsDbContext())
            { 
                var order = await context.Orders.FindAsync(_currentOrderId);
                if (order != null)

                {
                    order.isSentToKitchen = true;
                    await context.SaveChangesAsync();
                }
                ToastControl.Show("تم الحفظ", " تم حفظ الطلب بنجاح وارساله الى المطبخ", ToastControl.NotificationType.Success);
                var details = context.OrderDetails.Where(d =>d.OrderId ==  _currentOrderId).Select(d => d.Product.IsKitchenItem).ToList();
                if (details.Any())
                {
                    foreach (var item in details)
                    {
                        if (item != false)
                        {
                            return;
                        }
                    }
                    order.isReady = true;
                    details.RemoveRange(0,details.Count);
                    await context.SaveChangesAsync();
                }

            }
            
        }





        private async Task SaveChangesToDatabaseAsync()
        {
            using (var context = new VinceSweetsDbContext())
            using (var tx = await context.Database.BeginTransactionAsync())
            {
                try
                {
                    bool hasActiveDetails = CartItems.Any();
                    if (_deletedDetailsIds.Any())
                    {
                        var toDelete = await context.OrderDetails
                            .Where(d => _deletedDetailsIds.Contains(d.Id))
                            .ToListAsync();

                        foreach (var item in toDelete)
                            item.isDeleted = true;
                    }
                    foreach (var item in CartItems)
                    {
                        if (item.OrderDetailId == 0)
                        {
                            var newDetail = new OrderDetail
                            {
                                OrderId = _currentOrderId,
                                ProductId = item.ProductId,
                                ProductName = item.Name,
                                Price = item.Price,
                                Quantity = item.Quantity,
                                isDeleted = false
                            };
                            context.OrderDetails.Add(newDetail);
                        }
                        else
                        {
                            var existingDetail = await context.OrderDetails.FindAsync(item.OrderDetailId);
                            if (existingDetail != null)
                            {
                                existingDetail.Quantity = item.Quantity;
                                existingDetail.isDeleted = false; // إعادة تفعيل لو كان محذوف سابقاً
                            }
                        }
                    }

                    // 3) تحميل الطلب
                    var order = await context.Orders.FindAsync(_currentOrderId);

                    if (order != null)
                    {
                        if (_parentOrderId != null)
                            order.ParentOrderId = _parentOrderId;

                        // تحديث المجموع من السلة
                        var currentOrderTotal = CartItems.Sum(x => x.TotalPrice);
                        order.TotalAmount = currentOrderTotal;

                        // 4) تحديث حالة الطاولة + Soft delete للطلب عند الفراغ
                        if (order.TableId.HasValue)
                        {
                            var table = await context.RestaurantTables.FindAsync(order.TableId.Value);
                            if (table != null)
                            {
                                table.Status = hasActiveDetails ? 1 : 0; // Busy / Free
                            }
                        }

                        if (!hasActiveDetails)
                        {
                            // ✅ سوفت ديليت لكل تفاصيل الطلب (حتى لو لم تمر عبر _deletedDetailsIds)
                            var allDetails = await context.OrderDetails
                                .Where(d => d.OrderId == _currentOrderId && !d.isDeleted)
                                .ToListAsync();

                            foreach (var d in allDetails)
                                d.isDeleted = true;

                            // ✅ سوفت ديليت للطلب نفسه
                            order.isDeleted = true;

                            // (اختياري) صفّر الإجمالي
                            order.TotalAmount = 0;
                        }
                        else
                        {
                            // ✅ إذا أصبح فيه عناصر ثانية، تأكد أن الطلب ليس محذوف
                            order.isDeleted = false;
                        }
                    }

                    await context.SaveChangesAsync();
                    await tx.CommitAsync();

                    _isDirty = false;
                    
                    _deletedDetailsIds.Clear();

                    await LoadExistingOrderDetailsAsync();
                }
                catch
                {
                    await tx.RollbackAsync();
                    throw;
                }
            }
        }







        private async void PayButton_Click(object sender, RoutedEventArgs e)

        {

            if (btnPay.Content.ToString() == "إغلاق")

            {

                Close();

                return;

            }



            if (!CartItems.Any()) return;



            

            _subTotalAmount = CartItems.Sum(i => i.TotalPrice);

            _discountAmount = 0; 



            

            _currentInput = "0";

            UpdatePaymentCalculations(); 



            PaymentOverlay.Visibility = Visibility.Visible;

        }



        private async void PrintReceipt_Click(object sender, RoutedEventArgs e)

        {

           

            decimal.TryParse(_currentInput, out var paid);

            if (paid < _totalAmountToPay) 

            {

                ToastControl.Show("خطأ", "المبلغ غير كافي", ToastControl.NotificationType.Error);

                return;

            }

            if (_discountAmount > (_totalAmountToPay * (decimal)0.15) && CurrentUser.Role == (int)UserRole.Cashier)

            {
                ToastControl.Show("تنبيه", "مبلغ الخصم كبير!", ToastControl.NotificationType.Warning);
                //MessageBox.Show("مبلغ الخصم كبير!", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);

                return;

            }

            try

            {

                await SaveChangesToDatabaseAsync();



                using (var context = new VinceSweetsDbContext())

                {

                    var order = await context.Orders.FindAsync(_currentOrderId);

                    if (order != null)

                    {

                        order.isPaid = true;
                        wasPaid = true;
                        order.isSentToKitchen = true;


                        order.DiscountAmount = _discountAmount;


                    }


                    if (_currentTableId.HasValue)

                    {

                        var table = await context.RestaurantTables.FindAsync(_currentTableId.Value);

                        if (table != null) table.Status = 2;

                    }

                    await context.SaveChangesAsync();

                }




                TicketPrinter Pr = new TicketPrinter();

                Pr.Print(_currentOrderId, false);



                // ... (إغلاق النافذة) ...
                ToastControl.Show("تم الدفع", "تم الدفع بنجاح", ToastControl.NotificationType.Success);
                //MessageBox.Show("تم الدفع بنجاح.", "Paid", MessageBoxButton.OK, MessageBoxImage.Information);

                PaymentOverlay.Visibility = Visibility.Collapsed;
                this.DialogResult = wasPaid;
                Close();

            }

            catch (Exception ex)

            {

                Log.Error("Error in print MainWindow");

            }

        }

        // ===================== الخروج (الحماية) =====================

        private async void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isReadOnly) { Close(); return; }

            if (_isDirty)
            {
                if(MessageBox.Show("هناك تغييرات لم تقم بحفظها\nهل تود الخروج واهمالها؟\nنعم للخروج\nلا للبقاء في هذه الصفحه","تحذير",MessageBoxButton.YesNo,MessageBoxImage.Information) == MessageBoxResult.No) {return; }
                
            }
            try
            {
                using (var context = new VinceSweetsDbContext())
                {
                    var currentOrder = await context.Orders.FindAsync(_currentOrderId);

                    // هل يوجد تفاصيل فعّالة (غير محذوفة)؟
                    bool hasDetails = await context.OrderDetails
                        .AnyAsync(d => d.OrderId == _currentOrderId && !d.isDeleted);
                    if (currentOrder != null && !hasDetails)
                    {
                        // ✅ تحرير الطاولة إذا كان الطلب لطاولة
                        if (currentOrder.TableId.HasValue)
                        {
                            var table = await context.RestaurantTables.FindAsync(currentOrder.TableId.Value);
                            if (table != null)
                                table.Status = 0; // Free
                        }

                        // حذف الطلب الفارغ
                        context.Orders.Remove(currentOrder);

                        await context.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                ToastControl.Show("خطأ", "حدث خطأ في البرنامج!", ToastControl.NotificationType.Error);

                Log.Error(ex, "Table window fault");
            }

            Close();
        }




        // دوال المساعدة للواجهة

        private void FilterProducts(string categoryName)

        {

            DisplayedProducts.Clear();

            var filtered = (categoryName == "الكل" || string.IsNullOrWhiteSpace(categoryName)) ? AllProducts : AllProducts.Where(p => p.Category?.Name == categoryName);

            foreach (var p in filtered) DisplayedProducts.Add(p);

        }

        private void ShowLoading(bool show, string? hint = null)

        {

            LoadingOverlay.Visibility = show ? Visibility.Visible : Visibility.Collapsed;

            if (!string.IsNullOrWhiteSpace(hint))

                txtLoadingHint.Text = hint;

        }

        private void Category_Click(object sender, RoutedEventArgs e) { if (sender is Button btn && btn.Tag is string c) FilterProducts(c); }

        private void CalculateTotal() { txtTotal.Text = $"{CartItems.Sum(i => i.TotalPrice):N0} د.ع"; }

        private void CloseOverlay_Click(object sender, RoutedEventArgs e) => PaymentOverlay.Visibility = Visibility.Collapsed;

        private void Numpad_Click(object sender, RoutedEventArgs e) { if (sender is Button b) { _currentInput = _currentInput == "0" ? b.Content.ToString() : _currentInput + b.Content.ToString(); UpdatePaymentCalculations(); } }

        private void Numpad_Clear_Click(object sender, RoutedEventArgs e) { _currentInput = "0"; UpdatePaymentCalculations(); }

        private void Numpad_Backspace_Click(object sender, RoutedEventArgs e) { _currentInput = _currentInput.Length > 1 ? _currentInput[..^1] : "0"; UpdatePaymentCalculations(); }

        private void UpdatePaymentCalculations()

        {

            // 1. حساب الصافي

            _totalAmountToPay = _subTotalAmount - _discountAmount;

            if (_totalAmountToPay < 0) _totalAmountToPay = 0; // لا يجوز أن يكون بالسالب



            // 2. تحديث نصوص الواجهة

            txtSubTotal.Text = _subTotalAmount.ToString("N0");

            txtDiscountDisplay.Text = _discountAmount.ToString("N0");

            txtOverlayTotal.Text = _totalAmountToPay.ToString("N0");



            // 3. معالجة المدفوع والباقي

            decimal.TryParse(_currentInput, out var paid);

            txtPaidAmount.Text = paid.ToString("N0");



            var change = paid - _totalAmountToPay;

            txtChangeAmount.Text = change < 0 ? "غير كافٍ" : change.ToString("N0");

            txtChangeAmount.Foreground = change < 0 ? Brushes.Red : Brushes.Green;

        }

        private void BtnEditDiscount_Click(object sender, RoutedEventArgs e)

        {

            // استدعاء النافذة الجديدة المخصصة للمس

            var touchWindow = new DiscountInputWindow(_subTotalAmount);



            // تظليل الخلفية للتركيز

            this.Opacity = 0.5;



            if (touchWindow.ShowDialog() == true)

            {

                _discountAmount = touchWindow.DiscountValue;

                UpdatePaymentCalculations();

            }



            this.Opacity = 1;

        }





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