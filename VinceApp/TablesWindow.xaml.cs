using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using VinceApp.Data;
using VinceApp.Data.Models;
using VinceApp.Pages;


namespace VinceApp
{
    public partial class TablesWindow : Window
    {
        private const int TABLE_FREE = 0;
        private const int TABLE_BUSY = 1;
        private const int TABLE_PAID = 2;
        private int? _ParentOrderID = null;
        private bool _initialized = false;

        public TablesWindow()
        {
            InitializeComponent();
            ToastControl.Show("Welcome", $"تم تسجيل الدخول بأسم : {CurrentUser.FullName}", ToastControl.NotificationType.Success);


        }
        private async Task LoadTables()
        {
            TablesPanel.Children.Clear();
            if (TakeawayPanel != null) TakeawayPanel.Children.Clear();

            try
            {
                using (var context = new VinceSweetsDbContext())
                {
                    if (!await context.Database.CanConnectAsync())
                    {
                        MessageBox.Show("لا يمكن الاتصال بقاعدة البيانات.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (!await context.RestaurantTables.AnyAsync())
                    {
                        for (int i = 1; i <= 12; i++)
                            context.RestaurantTables.Add(new RestaurantTable { TableNumber = i, Status = TABLE_FREE });
                        await context.SaveChangesAsync();
                    }

                    // رسم الطاولات (لم يتغير)
                    var tables = await context.RestaurantTables.AsNoTracking().OrderBy(t => t.TableNumber).ToListAsync();
                    foreach (var table in tables)
                    {
                        // ... (كود رسم الطاولات كما هو بدون تغيير) ...
                        Button btnTable = new Button
                        {
                            Width = 250,
                            Height = 250,
                            Margin = new Thickness(10),
                            FontSize = 24,
                            FontWeight = FontWeights.Bold,
                            Tag = table
                        };
                        btnTable.Click += Table_Click;

                        if (table.Status == TABLE_FREE)
                        {
                            btnTable.Content = $"طاولة {table.TableNumber}\n(فارغة)";
                            btnTable.Background = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                        }
                        else if (table.Status == TABLE_BUSY)
                        {
                            btnTable.Content = $"طاولة {table.TableNumber}\n(مشغولة)";
                            btnTable.Background = new SolidColorBrush(Color.FromRgb(244, 67, 54));
                        }
                        else if (table.Status == TABLE_PAID)
                        {
                            btnTable.Content = $"طاولة {table.TableNumber}\n(مدفوع)";
                            btnTable.Background = new SolidColorBrush(Color.FromRgb(255, 152, 0));
                        }
                        btnTable.Foreground = Brushes.White;
                        TablesPanel.Children.Add(btnTable);
                    }

                    // =========================================================
                    // التعديل الجذري: منطق الطلبات السفرية (Booleans)
                    // =========================================================
                    if (TakeawayPanel != null)
                    {
                        // 1. التعديل في الاستعلام: نجلب أي طلب لم يتم تسليمه بعد (بغض النظر عن الدفع والجاهزية)
                        var takeawayOrders = await context.Orders
                            .Where(o => o.TableId == null && !o.isServed)
                            .OrderByDescending(o => o.OrderDate)
                            .ToListAsync();

                        foreach (var order in takeawayOrders)
                        {
                            // قراءة الحالات
                            bool isPaid = order.isPaid;
                            bool isReady = order.isReady; // جاهز من المطبخ

                            Button btnTakeaway = new Button
                            {
                                Height = 80,
                                Margin = new Thickness(0, 5, 0, 5),
                                Foreground = Brushes.White,
                                Tag = order.Id,
                            };

                            // 2. التعديل في الألوان: إضافة الحالة البرتقالية (جاهز)
                            if (isPaid)
                            {
                                // أخضر غامق (مدفوع) - الأولوية القصوى
                                btnTakeaway.Background = new SolidColorBrush(Color.FromRgb(46, 125, 50));
                                btnTakeaway.BorderBrush = Brushes.LightGreen;
                            }
                            else if (isReady)
                            {
                                // برتقالي (جاهز ولكن غير مدفوع) - انتبه هنا
                                btnTakeaway.Background = new SolidColorBrush(Color.FromRgb(255, 152, 0));
                                btnTakeaway.BorderBrush = Brushes.OrangeRed;
                            }
                            else
                            {
                                // بنفسجي (غير جاهز وغير مدفوع)
                                btnTakeaway.Background = new SolidColorBrush(Color.FromRgb(106, 27, 154));
                                btnTakeaway.BorderBrush = Brushes.Purple;
                            }
                            btnTakeaway.BorderThickness = new Thickness(2);

                            // النصوص
                            string statusText;
                            if (isPaid) statusText = "✅ (مدفوع)";
                            else if (isReady) statusText = "🔔 (جاهز)";
                            else statusText = "⏳ (تحضير)";

                            string time = order.OrderDate?.ToString("hh:mm tt") ?? "";

                            var stack = new StackPanel { Orientation = Orientation.Vertical };
                            stack.Children.Add(new TextBlock { Text = $"📦 #{order.OrderNumber} {statusText}", FontWeight = FontWeights.Bold, FontSize = 16, HorizontalAlignment = HorizontalAlignment.Center });
                            stack.Children.Add(new TextBlock { Text = $"{time}", FontSize = 12, Foreground = Brushes.LightGray, HorizontalAlignment = HorizontalAlignment.Center });
                            btnTakeaway.Content = stack;

                            // 3. التعديل في الحدث (المنطق المبسط)
                            btnTakeaway.Click += async (s, e) =>
                            {
                                if (s is Button b && b.Tag is int orderId)
                                {
                                    // القاعدة الذهبية:
                                    // إذا لم يدفع (سواء كان جاهزاً أم لا) -> افتح الكاشير للدفع
                                    if (!isPaid)
                                    {
                                        OpenCashierWindow(orderId, null, null, null);
                                    }
                                    // إذا دفع -> افتح الخيارات (تسليم/إضافة)
                                    else
                                    {
                                        var dialog = new TakeawayOptionsWindow();
                                        dialog.ShowDialog();

                                        if (dialog.UserChoice == "Serve")
                                        {
                                            await CompleteOrderAsync(orderId);
                                        }
                                        else if (dialog.UserChoice == "View")
                                        {
                                            OpenCashierWindow(orderId, null, null, null);
                                        }
                                        else if (dialog.UserChoice == "Add")
                                        {
                                            using (var ctx = new VinceSweetsDbContext())
                                            {
                                                var childOrder = new Order
                                                {
                                                    OrderNumber = await GenerateDailyOrderNumber(ctx),
                                                    TableId = null,
                                                    isPaid = false,
                                                    isSentToKitchen = false,
                                                    isReady = false,
                                                    isServed = false,
                                                    OrderDate = DateTime.Now,
                                                    TotalAmount = 0,
                                                    ParentOrderId = orderId
                                                };
                                                ctx.Orders.Add(childOrder);
                                                await ctx.SaveChangesAsync();
                                                OpenCashierWindow(childOrder.Id, null, null, orderId);
                                            }
                                        }
                                    }
                                }
                            };

                            TakeawayPanel.Children.Add(btnTakeaway);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "error with LoadTable() in tableswindow");
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        public void ApplyPermissions()
        {
            if (CurrentUser.Role == (int)UserRole.Cashier)
            {
                btnAdmin.IsEnabled = false;
            }
        }
        private async Task CompleteOrderAsync(int orderId)
        {
            if (MessageBox.Show("هل استلم الزبون الطلب؟\nسيتم إخفاء الطلب من القائمة.", "تأكيد التسليم", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    using (var context = new VinceSweetsDbContext())
                    {
                        var order = await context.Orders.FindAsync(orderId);
                        if (order != null)
                        {
                            order.isServed = true; // حالة جديدة تعني انتهى
                            await context.SaveChangesAsync();
                        }
                    }
                    await LoadTables(); // تحديث الشاشة
                }
                catch(Exception ex) { MessageBox.Show("فشل التحديث","فشل",MessageBoxButton.OK,MessageBoxImage.Error); Log.Error(ex, "error with CompleteOrderAsync in tableswindow"); }
            }
        }

       
        private async void Table_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var table = btn?.Tag as RestaurantTable;
            if (table == null) return;

            btn.IsEnabled = false;

            try
            {
                // ================= التعامل مع الطاولة المدفوعة =================
                if (table.Status == TABLE_PAID)
                {
                    var dialog = new TableActionWindow();
                    dialog.ShowDialog();

                    if (dialog.UserChoice == "Cancel")
                    {
                        btn.IsEnabled = true;
                        return;
                    }

                    
                    if (dialog.UserChoice == "View")
                    {
                        using (var context = new VinceSweetsDbContext())
                        {
                            // نبحث عن الطلب المدفوع المرتبط بهذه الطاولة
                            // نأخذ الأحدث (OrderByDescending) تحسباً لوجود أكثر من طلب قديم
                            var paidOrder = await context.Orders
                                .OrderByDescending(o => o.OrderDate)
                                .FirstOrDefaultAsync(o => o.TableId == table.Id && (o.isPaid));

                            if (paidOrder != null)
                            {
                                // نفتح النافذة (وبما أن الحالة Paid، ستفتح للقراءة فقط تلقائياً)
                                OpenCashierWindow(paidOrder.Id, table.Id,table.TableName,null);
                            }
                            else
                            {
                                MessageBox.Show("لم يتم العثور على الفاتورة المدفوعة!", "خطأ");
                            }
                        }
                        btn.IsEnabled = true;
                        return; // نخرج هنا ولا ننشئ طلباً جديداً
                    }

                    // --- خيار 2: إخلاء الطاولة ---
                    if (dialog.UserChoice == "Clear")
                    {
                        if (MessageBox.Show("سيتم اخلاء هذه الطاولة \n هل انت متاكد؟", "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                        {
                            using (var context = new VinceSweetsDbContext())
                            {
                                var dbTable = await context.RestaurantTables.FindAsync(table.Id);
                                if (dbTable != null)
                                {
                                    dbTable.Status = TABLE_FREE;
                                    
                                    await context.SaveChangesAsync();
                                }
                            }
                            await LoadTables();
                            btn.IsEnabled = true;
                            return;
                        }
                        else
                        {
                            // إذا تراجع عن الإخلاء
                            btn.IsEnabled = true;
                            return;
                        }
                    }

                    // --- خيار 3: طلب جديد ---
                    if (dialog.UserChoice == "NewOrder")
                    {
                        using var context = new VinceSweetsDbContext();
                        var ParentOrder = await context.Orders
                                .OrderByDescending(o => o.OrderDate)
                                .FirstOrDefaultAsync(o => o.TableId == table.Id && (o.isPaid));
                        _ParentOrderID = ParentOrder?.Id;
                        table.Status = TABLE_FREE;
                    }
                }

                // ================= المنطق العادي (إنشاء/فتح طلب) =================
                int orderId;

                using (var context = new VinceSweetsDbContext())
                {
                    using (var tx = await context.Database.BeginTransactionAsync())
                    {
                        if (table.Status == TABLE_FREE)
                        {
                            // إنشاء طلب جديد
                            var newOrder = new Order
                            {
                                OrderNumber = await GenerateDailyOrderNumber(context),
                                TableId = table.Id,
                                isPaid = false,
                                isServed = false,
                                isReady = false,
                                isSentToKitchen = false,
                                OrderDate = DateTime.Now,
                                TotalAmount = 0,
                                ParentOrderId = _ParentOrderID
                            };

                            context.Orders.Add(newOrder);
                            // ⚠️ ملاحظة: لا نغير حالة الطاولة هنا (كما اتفقنا سابقاً)
                            // ستتغير فقط عند ضغط "حفظ" داخل الكاشير

                            await context.SaveChangesAsync();
                            await tx.CommitAsync();
                            _ParentOrderID = null;
                            orderId = newOrder.Id;
                        }
                        else
                        {
                            // فتح طلب مفتوح موجود
                            var existingOrder = await context.Orders.FirstOrDefaultAsync(o =>
                                o.TableId == table.Id && !o.isPaid);

                            if (existingOrder == null)
                            {
                                // حالة نادرة: الطاولة مشغولة لكن لا يوجد طلب مفتوح (خطأ بيانات)
                                // نصفر الطاولة ونعيد التحميل
                                var dbTable = await context.RestaurantTables.FindAsync(table.Id);
                                if (dbTable != null) { dbTable.Status = TABLE_FREE; await context.SaveChangesAsync(); }
                                await LoadTables();
                                btn.IsEnabled = true;
                                return;
                            }
                            orderId = existingOrder.Id;
                        }
                    }
                }

                 OpenCashierWindow(orderId, table.Id,table.TableName,null);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "error in tableswindow tableclick()");
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (btn != null) btn.IsEnabled = true;
            }
        }

        // ============================
        // طلب سفري جديد
        // ============================
        private async void TakeawayButton_Click(object sender, RoutedEventArgs e )
        {
            
            var btn = sender as Button;
            if (btn != null) btn.IsEnabled = false;
            
            try
            {
                _ParentOrderID = null;
                int orderId;
                await Task.Yield();
                using (var context = new VinceSweetsDbContext())
                using (var tx = await context.Database.BeginTransactionAsync())
                {
                    var newOrder = new Order
                    {
                        OrderNumber = await GenerateDailyOrderNumber(context),
                        TableId = null, // سفري
                        isPaid = false,
                        isSentToKitchen = false,
                        isReady = false,
                        isServed = false,
                        OrderDate = DateTime.Now,
                        TotalAmount = 0,
                        ParentOrderId = null
                    };

                    context.Orders.Add(newOrder);
                    await context.SaveChangesAsync();
                    await tx.CommitAsync();

                    orderId = newOrder.Id;
                }
                
                OpenCashierWindow(orderId, null,null,null);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "error at Takeaway in tableswindow");
                MessageBox.Show($"تعذر إنشاء طلب سفري.", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (btn != null) btn.IsEnabled = true;
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            _ = LoadTables();
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            var fileName = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;

            // تشغيل نسخة جديدة من البرنامج
            System.Diagnostics.Process.Start(fileName);

            // إغلاق النسخة الحالية فوراً
            Application.Current.Shutdown();
        }
        private void OpenAdmin_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentUser.Role == (int)UserRole.Cashier)
                return;

            AdminWindow admin = new AdminWindow();
            admin.ShowDialog();
            ApplyPermissions();
        }
        // ============================
        // أدوات مساعدة
        // ============================
        private async Task OpenCashierWindow(int orderId, int? tableId, string? tableName, int? parentOrderId = null)
        {
            MainWindow cashier = null;

            try
            {
                cashier = new MainWindow(orderId, tableId, tableName, parentOrderId)
                {
                    Owner = this
                };

                // نخفي بعد ما نحدد الـ Owner
                this.Hide();

                cashier.ShowDialog();
                if (cashier.wasPaid)
                {
                    ToastControl.Show("تم الدفع", "تم الدفع بنجاح", ToastControl.NotificationType.Success);
                }
            }
            finally
            {
                // نضمن رجوع نافذة الطاولات حتى لو صار استثناء
                this.Show();
                this.Activate();

                await LoadTables();
            }
        }

        private void ShowLoading(bool show, string? hint = null)
        {
            LoadingOverlay.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            if (!string.IsNullOrWhiteSpace(hint))
                txtLoadingHint.Text = hint;
        }

        private async Task<int> GenerateDailyOrderNumber(VinceSweetsDbContext context)
        {
            var count = await context.Orders.CountAsync(o =>
                o.OrderDate.HasValue &&
                o.OrderDate.Value.Date == DateTime.Now.Date);
            return count + 1;
        }

        private async void Window_ContentRendered(object sender, EventArgs e)
        {
            if (_initialized) return;
            _initialized = true;

            try
            {
                ShowLoading(true, "جاري تحميل الطاولات...");
                await Task.Yield();

                await LoadTables();
                ApplyPermissions();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "error in Window_ContentRendered");
                MessageBox.Show("حدث خطأ أثناء تحميل البيانات.", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ShowLoading(false);
            }
        }


        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingPage = new SettingsPage();
            settingPage.OnNotificationReqested += (title,body) =>
            {
                ToastControl.Show(title,body, ToastControl.NotificationType.Info);
            };
            Window window = new Window
            {
                Title = "الإعدادات",
                Content = settingPage, // نضع صفحة الإعدادات بداخلها
                Height = 750, // طول مناسب للتبويبات
                Width = 800,
                WindowStartupLocation = WindowStartupLocation.CenterScreen, // تظهر في المنتصف
                ResizeMode = ResizeMode.NoResize, // منع تغيير الحجم (اختياري)
                FlowDirection = FlowDirection.RightToLeft // لضمان اتجاه النافذة
                ,Owner = this
            };

            // 3. عرض النافذة بشكل Dialog (يمنع النقر على الخلفية حتى يتم إغلاق الإعدادات)
            window.ShowDialog();
        }

        
    }
}