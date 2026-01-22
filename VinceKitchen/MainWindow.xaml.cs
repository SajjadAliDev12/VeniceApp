using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using VinceApp.Data;


namespace VinceKitchen
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer _timer;
        private bool _isBusy = false;

        public MainWindow()
        {
            InitializeComponent();

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(5);
            _timer.Tick += async (s, e) => await LoadKitchenOrders();
            _timer.Start();

            

            _ = LoadKitchenOrders();
        }

        private async Task LoadKitchenOrders()
        {
            if (_isBusy) return;
            _isBusy = true;

            try
            {
                // نستخدم Task.Run لضمان أن عملية الاتصال بالكامل تتم في الخلفية
                // ولا تلمس واجهة المستخدم إلا عندما تجهز البيانات تماماً
                var displayList = await Task.Run(async () =>
                {
                    using (var context = new VinceSweetsDbContext())
                    {
                        
                        var tables = await context.RestaurantTables.ToListAsync();

                        // 2. جلب الطلبات (بشكل غير متزامن)
                        var activeOrders = await context.Orders
                            .Include(o => o.OrderDetails)
                            .ThenInclude(d => d.Product)
                            .Where(o => (o.isPaid || o.isSentToKitchen)
            && o.OrderDetails.Any(d => d.Product.IsKitchenItem == true && !d.IsServed && !d.isDeleted))

                            .OrderBy(o => o.OrderDate)
                            .ToListAsync();

                        // 3. تجهيز القائمة في الذاكرة (RAM)
                        // هذه العملية سريعة جداً ولا تحتاج اتصال
                        var list = new List<KitchenOrderViewModel>();

                        foreach (var order in activeOrders)
                        {
                            var pendingItems = order.OrderDetails
                                .Where(d => d.Product.IsKitchenItem == true && !d.IsServed && !d.isDeleted)
                                .Select(d => new KitchenItemView
                                {
                                    ProductName = d.ProductName,
                                    Quantity = d.Quantity
                                })
                                .ToList();

                            if (pendingItems.Any())
                            {
                                // البحث عن اسم الطاولة من القائمة المحملة مسبقاً (في الذاكرة)
                                // بدلاً من الاتصال بالداتا بيس (Find)
                                var tableObj = tables.FirstOrDefault(t => t.Id == order.TableId);
                                string tableName = tableObj != null ? $"طاولة {tableObj.TableNumber}" : "📦 سفري";
                                if (order.ParentOrderId != null)
                                    tableName += " - ملحق";
                                list.Add(new KitchenOrderViewModel
                                {
                                    OrderId = order.Id,
                                    TableNumberDisplay = tableName,
                                    OrderTime = order.OrderDate ?? DateTime.Now,
                                    KitchenItems = pendingItems
                                });
                            }
                        }
                        return list;
                    }
                });

                // 4. الآن فقط نعود للواجهة الرئيسية لتحديث الشاشة
                OrdersList.ItemsSource = displayList;

                if (StatusBorder != null) StatusBorder.Background = Brushes.LimeGreen;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in kitchen screen LoadOrders()");
                if (StatusBorder != null)
                {
                    StatusBorder.Background = Brushes.Red;
                    
                }
            }
            finally
            {
                _isBusy = false;
            }
        }

        private async void OrderDone_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(
                "الطلب جاهز\nسوف يتم حذف هذا الطلب من قائمة الانتظار",
                "تأكيد",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Information) == MessageBoxResult.OK)
            {
                // وضعنا زر "جاهز" أيضاً داخل Task.Run لنضمن عدم تجميد الشاشة أثناء الحفظ
                await Task.Run(async () =>
                {
                    try
                    {
                        if (sender is Button btn || (sender is FrameworkElement fe && fe.Tag is int))
                        {
                            // التقاط الـ ID يحتاج لتمرير آمن، لكن بما أن sender عنصر UI
                            // علينا الحذر. الأفضل تمرير الـ ID للدالة، لكن للتبسيط سنقوم بالعملية داخل الـ Dispatcher للجزء الخاص بالـ UI
                            // ثم الحفظ في الخلفية.
                        }
                    }
                    catch { /* تجاهل */ }
                });

                // الطريقة الأبسط والأكثر أماناً لزر "جاهز" (بدون تعقيد Task.Run الزائد للعناصر UI):
                // نستخدم المنطق السابق مع التأكد من الـ Async
                try
                {
                    if (sender is Button btn && btn.Tag is int orderId)
                    {
                        bool success = await Task.Run(async () =>
                        {
                            using (var context = new VinceSweetsDbContext())
                            {
                                var order = await context.Orders
                                    .Include(o => o.OrderDetails)
                                    .ThenInclude(d => d.Product)
                                    .FirstOrDefaultAsync(o => o.Id == orderId);

                                if (order != null)
                                {
                                    var itemsToServe = order.OrderDetails
                                        .Where(d => d.Product.IsKitchenItem == true && !d.IsServed)
                                        .ToList();

                                    foreach (var item in itemsToServe)
                                    {
                                        item.IsServed = true;
                                    }
                                    order.isReady = true;
                                    await context.SaveChangesAsync();
                                    return true;
                                }
                                return false;
                            }
                        });

                        if (success)
                        {
                            await LoadKitchenOrders();
                        }
                    }
                }
                catch
                {

                    MessageBox.Show("فشل الاتصال بالسيرفر.", "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
                    if (StatusBorder != null) { StatusBorder.Background = Brushes.Red;
                        
                    }
                }
            }
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow settingsWin = new SettingsWindow();
            settingsWin.Owner = this; // لجعل النافذة تابعة للشاشة الرئيسية
            settingsWin.ShowDialog(); // يفتحها كـ Modal (يمنع استخدام الخلفية حتى تغلق هذه)
        }
    }

    // ViewModels (كما هي)
    public class KitchenOrderViewModel
    {
        public int OrderId { get; set; }
        public string TableNumberDisplay { get; set; }
        public DateTime OrderTime { get; set; }
        public List<KitchenItemView> KitchenItems { get; set; }

        public string TimeDisplay
        {
            get
            {
                var diff = DateTime.Now - OrderTime;
                if (diff.TotalHours < 1)
                    return $"{diff.Minutes} دقيقة";
                else
                    return $"{(int)diff.TotalHours} س و {diff.Minutes} د";
            }
        }
    }

    public class KitchenItemView
    {
        public string ProductName { get; set; }
        public int Quantity { get; set; }
    }
}