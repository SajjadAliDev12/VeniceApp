using AutoUpdaterDotNET;
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
using System.Windows.Threading;
using VinceApp.Data;
using VinceApp.Data.Enums;
using VinceApp.Data.Models;
using VinceApp.Pages;
using VinceApp.Toters_Feature;

namespace VinceApp
{
    public partial class TablesWindow : Window
    {
        private bool _isLoading = false;
        private const int TABLE_FREE = 0;
        private const int TABLE_BUSY = 1;
        private const int TABLE_PAID = 2;
        private int? _ParentOrderID = null;
        private bool _initialized = false;
        private readonly System.Collections.Generic.List<(TextBlock TextBlock, DateTime StartTime)> _activeTimers = new();
        private DispatcherTimer _globalClockTimer;
        public TablesWindow()
        {
            InitializeComponent();
            ToastControl.Show("Welcome", $"تم تسجيل الدخول بأسم : {CurrentUser.FullName}", ToastControl.NotificationType.Success);
        }
        public class BusyTable
        {
            public int? tableID { get; set; }
            public decimal? TotalPrice { get; set; }
            public DateTime? OrderDate { get; set; }
        }
        private async Task LoadTables()
        {
            if (_isLoading) return;
            _isLoading = true;
            _activeTimers.Clear();
            TablesPanel.Children.Clear();
           
            if (TakeawayPanel != null) TakeawayPanel.Children.Clear();

            try
            {
                using (var context = new VinceSweetsDbContext())
                {
                    var tables = await context.RestaurantTables
                        .AsNoTracking()
                        .OrderBy(t => t.TableNumber)
                        .ToListAsync();

                    // ✅ 1) الطاولات المشغولة
                    var busyTableIds = await context.Orders
                        .Where(o => o.TableId != null && !o.isPaid && !o.isServed && !o.isDeleted)
                        .Select(o => new BusyTable
                        {
                            tableID = o.TableId,
                            TotalPrice = o.TotalAmount,
                            OrderDate = o.OrderDate,
                        } )
                        .Distinct()
                        .ToListAsync();

                    // ✅ 2) الطاولات المدفوعة
                    var paidTableIds = await context.Orders
                        .Where(o => o.TableId != null && o.isPaid && !o.isDeleted && !o.isDone)
                        .GroupBy(o => o.TableId!.Value)
                        .Select(g => new BusyTable
                        {
                            tableID = g.Key,
                            TotalPrice =g.Sum(x => x.TotalAmount),
                            OrderDate = g.Max(x => x.OrderDate),
                        })
                        .ToListAsync();

                    var busySet = busyTableIds.ToDictionary(x => x.tableID,x => new { x.TotalPrice, x.OrderDate });
                    var paidSet = paidTableIds.ToDictionary(x => x.tableID, x => new { x.TotalPrice, x.OrderDate });

                    foreach (var table in tables)
                    {
                        int computedStatus =
                            busySet.ContainsKey(table.Id) ? TABLE_BUSY :
                            paidSet.ContainsKey(table.Id) ? TABLE_PAID :
                            TABLE_FREE;

                        // تحديث الحالة لضمان عمل الكليك بشكل صحيح
                        table.Status = computedStatus;

                        Button btnTable = new Button
                        {
                            Width = 220,
                            Height = 210,
                            Margin = new Thickness(8),
                            Tag = table,
                            Cursor = Cursors.Hand,
                            Background = Brushes.White,
                            BorderThickness = new Thickness(5)
                        };

                        var borderStyle = new Style(typeof(Border));
                        borderStyle.Setters.Add(new Setter(Border.CornerRadiusProperty, new CornerRadius(12)));
                        btnTable.Resources.Add(typeof(Border), borderStyle);

                        var dropShadow = new System.Windows.Media.Effects.DropShadowEffect
                        {
                            Color = Colors.Black,
                            Direction = 270,
                            ShadowDepth = 2,
                            BlurRadius = 8,
                            Opacity = 0.1
                        };
                        btnTable.Effect = dropShadow;

                        btnTable.Click += Table_Click;

                        var stack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                        var txtIcon = new TextBlock { FontSize = 30, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 5) };
                        var txtNumber = new TextBlock { Text = $"طاولة {table.TableNumber}", FontSize = 18, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center };
                        var txtStatus = new TextBlock { FontSize = 16, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 5, 0, 0) };
                        var txtPrice = new TextBlock {FontSize = 16,FontWeight = FontWeights.Bold,HorizontalAlignment = HorizontalAlignment.Center,Margin = new Thickness(0, 5, 0, 0)};
                        var txtTimerDisplay = new TextBlock { FontSize = 14, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 4, 0, 0) };
                        // --- الألوان والحالات (عاد للطبيعي بدون ذكر الملحق) ---
                        if (computedStatus == TABLE_FREE)
                        {
                            btnTable.BorderBrush = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                            txtIcon.Text = "🍽️";
                            txtStatus.Text = "(فارغة)";
                            txtStatus.Foreground = Brushes.Gray;
                            txtNumber.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50));
                        }
                        else if (computedStatus == TABLE_BUSY)
                        {
                            btnTable.BorderBrush = new SolidColorBrush(Color.FromRgb(229, 57, 53));
                            txtIcon.Text = "⛔";
                            txtStatus.Text = "(مشغولة)";
                            txtStatus.Foreground = new SolidColorBrush(Color.FromRgb(229, 57, 53));
                            txtNumber.Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40));
                            txtPrice.Text = $"{busySet[table.Id].TotalPrice:N0} د.ع";
                            if (busySet[table.Id].OrderDate.HasValue)
                            {
                                txtTimerDisplay.Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40));
                                _activeTimers.Add((txtTimerDisplay, busySet[table.Id].OrderDate.Value));
                            }
                        }
                        else // TABLE_PAID
                        {
                            btnTable.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 167, 38));
                            txtIcon.Text = "💰";
                            txtStatus.Text = "(مدفوع)";
                            txtStatus.Foreground = new SolidColorBrush(Color.FromRgb(239, 108, 0));
                            txtNumber.Foreground = new SolidColorBrush(Color.FromRgb(239, 108, 0));
                            txtPrice.Text = $"{paidSet[table.Id].TotalPrice:N0} د.ع";
                            if (paidSet[table.Id].OrderDate.HasValue)
                            {
                                txtTimerDisplay.Foreground = new SolidColorBrush(Color.FromRgb(239, 108, 0));
                                _activeTimers.Add((txtTimerDisplay, paidSet[table.Id].OrderDate.Value));
                            }
                        }

                        stack.Children.Add(txtIcon);
                        stack.Children.Add(txtNumber);
                        stack.Children.Add(txtStatus);
                        if (computedStatus != TABLE_FREE)
                            { stack.Children.Add(txtPrice); stack.Children.Add(txtTimerDisplay); }
                        btnTable.Content = stack;

                        TablesPanel.Children.Add(btnTable);
                    }
                    UpdateAllLiveTimers();
                    InitializeGlobalClock();
                    // --- قسم الطلبات السفري ---
                    if (TakeawayPanel != null)
                    {
                        var takeawayOrders = await context.Orders
                            .Where(o => o.TableId == null && !o.isServed && !o.isDone && !o.isDeleted && o.OrderSource != Enums.OrderSource.EnToters)
                            .OrderByDescending(o => o.OrderDate)
                            .ToListAsync();

                        foreach (var order in takeawayOrders)
                        {
                            bool isPaid = order.isPaid;
                            bool isReady = order.isReady;

                            Button btnTakeaway = new Button
                            {
                                Height = 70,
                                Margin = new Thickness(0, 0, 0, 10),
                                Tag = order.Id,
                                Cursor = Cursors.Hand,
                                Background = Brushes.White,
                                BorderThickness = new Thickness(0, 0, 8, 0),
                                HorizontalContentAlignment = HorizontalAlignment.Stretch
                            };

                            var takeawayBorderStyle = new Style(typeof(Border));
                            takeawayBorderStyle.Setters.Add(new Setter(Border.CornerRadiusProperty, new CornerRadius(8)));
                            btnTakeaway.Resources.Add(typeof(Border), takeawayBorderStyle);

                            string statusIcon = "";
                            string statusText = "";
                            SolidColorBrush statusColor;

                            if (isPaid)
                            {
                                statusColor = new SolidColorBrush(Color.FromRgb(67, 160, 71));
                                btnTakeaway.BorderBrush = statusColor;
                                statusIcon = "✅";
                                statusText = "مدفوع";
                            }
                            else if (isReady)
                            {
                                statusColor = new SolidColorBrush(Color.FromRgb(251, 140, 0));
                                btnTakeaway.BorderBrush = statusColor;
                                statusIcon = "🔔";
                                statusText = "جاهز";
                            }
                            else
                            {
                                statusColor = new SolidColorBrush(Color.FromRgb(142, 36, 170));
                                btnTakeaway.BorderBrush = statusColor;
                                statusIcon = "⏳";
                                statusText = "تحضير";
                            }

                            string time = order.OrderDate?.ToString("hh:mm tt") ?? "";

                            var grid = new Grid();
                            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                            var infoStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                            infoStack.Children.Add(new TextBlock { Text = $"#{order.Id} {statusText}", FontWeight = FontWeights.Bold, FontSize = 20, Foreground = Brushes.Black, Margin = new Thickness(0, 0, 10, 0) });
                            infoStack.Children.Add(new TextBlock{Text = $"{order.TotalAmount:N0} د.ع",FontWeight = FontWeights.Bold,FontSize = 18,Foreground = Brushes.DarkGreen,Margin = new Thickness(0, 0, 10, 0)});
                            infoStack.Children.Add(new TextBlock { Text = statusIcon, FontSize = 18, VerticalAlignment = VerticalAlignment.Center });

                            var timeBlock = new TextBlock
                            {
                                Text = time,
                                FontSize = 18,
                                Foreground = Brushes.Gray,
                                VerticalAlignment = VerticalAlignment.Center,
                                HorizontalAlignment = HorizontalAlignment.Left
                            };
                            Grid.SetColumn(timeBlock, 0);
                            Grid.SetColumn(infoStack, 1);

                            grid.Children.Add(timeBlock);
                            grid.Children.Add(infoStack);

                            btnTakeaway.Content = grid;
                            btnTakeaway.Click += async (s, e) =>
                            {
                                if (s is not Button b || b.Tag == null) return;
                                if (!int.TryParse(b.Tag.ToString(), out int orderId)) return;

                                try
                                {
                                    bool isPaidNow = false;
                                    using (var ctx = new VinceSweetsDbContext())
                                    {
                                        var current = await ctx.Orders.AsNoTracking()
                                            .Where(o => o.Id == orderId)
                                            .Select(o => new { o.isPaid, o.isDone })
                                            .FirstOrDefaultAsync();

                                        if (current == null) { ToastControl.Show("تنبيه", "لم يتم العثور على الطلب", ToastControl.NotificationType.Warning); await LoadTables(); return; }
                                        if (current.isDone) { await LoadTables(); return; }
                                        isPaidNow = current.isPaid;
                                    }

                                    if (!isPaidNow)
                                    {
                                        await OpenCashierWindow(orderId, null, null, null);
                                        return;
                                    }

                                    var dialog = new TakeawayOptionsWindow();
                                    dialog.Owner = this;
                                    dialog.ShowDialog();

                                    if (dialog.UserChoice == "Serve") { await CompleteOrderAsync(orderId); return; }
                                    if (dialog.UserChoice == "View") { await OpenCashierWindow(orderId, null, null, null); return; }
                                    if (dialog.UserChoice == "Add")
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
                                                isDone = false,
                                                ParentOrderId = orderId
                                            };
                                            ctx.Orders.Add(childOrder);
                                            await ctx.SaveChangesAsync();
                                            await OpenCashierWindow(childOrder.Id, null, null, orderId);
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Log.Error(ex, "Takeaway click error");
                                    ToastControl.Show("خطأ", "حدث خطأ أثناء فتح الطلب", ToastControl.NotificationType.Error);
                                }
                            };

                            TakeawayPanel.Children.Add(btnTakeaway);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "error with LoadTable()");
                ToastControl.Show("خطأ", "حدث خطأ في البرنامج", ToastControl.NotificationType.Error);
            }
            finally { _isLoading = false; }
        }
        private void InitializeGlobalClock()
        {
            // إنشاء التايمر المركزي إذا لم يكن موجوداً لتحديث نصوص الساعات دورياً
            if (_globalClockTimer == null)
            {
                _globalClockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(60) }; // تحديث كل 60 ثانية (دقيقة) لضمان الأداء الصافي
                _globalClockTimer.Tick += (s, e) => UpdateAllLiveTimers();
                _globalClockTimer.Start();
            }
        }

        private void UpdateAllLiveTimers()
        {
            if (!_activeTimers.Any()) return;

            DateTime now = DateTime.Now;

            foreach (var item in _activeTimers)
            {
                TimeSpan elapsed = now - item.StartTime;

                // تنسيق الوقت ليظهر بشكل احترافي (ساعة : دقيقة)
                if (elapsed.TotalHours >= 1)
                {
                    item.TextBlock.Text = $"⏱️ {(int)elapsed.TotalHours} ساعة و {elapsed.Minutes} دقيقة";
                }
                else
                {
                    item.TextBlock.Text = $"⏱️ {elapsed.Minutes} دقيقة";
                }
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
            if (await MyConfirmDialog.ShowAsync("تأكيد التسليم","سيتم اخفاء الطلب من القائمه وتحديده كطلب مستلم"))
            {
                try
                {
                    using (var context = new VinceSweetsDbContext())
                    {
                        var order = await context.Orders.FindAsync(orderId);
                        if (order != null)
                        {
                            order.isServed = true;
                            order.isDone = true;
                            await context.SaveChangesAsync();
                        }
                    }
                    await LoadTables(); ToastControl.Show("تم اكمال الطلب", "تم اخفاء الطلب من القائمة ", ToastControl.NotificationType.Info);
                }
                catch (Exception ex)
                {
                    ToastControl.Show("فشل", "فشل التحديث", ToastControl.NotificationType.Error); Log.Error(ex, "error with CompleteOrderAsync in tableswindow");
                }
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
                    dialog.Owner = this;
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
                            var paidOrder = await context.Orders
                                .OrderByDescending(o => o.OrderDate)
                                .FirstOrDefaultAsync(o => o.TableId == table.Id && (o.isPaid));

                            if (paidOrder != null)
                            {
                                await OpenCashierWindow(paidOrder.Id, table.Id, table.TableName, null);
                            }
                            else
                            {
                                ToastControl.Show("خطأ", "لم يتم العثور على القائمة المدفوعه", ToastControl.NotificationType.Error);
                            }
                        }
                        btn.IsEnabled = true;
                        return;
                    }

                    // --- خيار 2: إخلاء الطاولة ---
                    if (dialog.UserChoice == "Clear")
                    {
                        var Result = await MyConfirmDialog.ShowAsync("تأكيد", "سيتم اخلاء هذه الطاولة هل انت متأكد؟");
                        if (Result)
                        {
                            using (var context = new VinceSweetsDbContext())
                            {
                                var activeOrders = await context.Orders
                                    .Where(o => o.TableId == table.Id && !o.isDone && !o.isDeleted)
                                    .ToListAsync();

                                foreach (var order in activeOrders)
                                {
                                    order.isDone = true;
                                    order.isServed = true;
                                }

                                var dbTable = await context.RestaurantTables.FindAsync(table.Id);
                                if (dbTable != null)
                                {
                                    dbTable.Status = TABLE_FREE;
                                }

                                await context.SaveChangesAsync();
                            }

                            await LoadTables();
                            ToastControl.Show("تم الاخلاء", "تم تصفية جميع طلبات الطاولة", ToastControl.NotificationType.Success);
                            btn.IsEnabled = true;
                            return;
                        }
                        else
                        {
                            btn.IsEnabled = true;
                            return;
                        }
                    }


                    if (dialog.UserChoice == "NewOrder")
                    {
                        using var context = new VinceSweetsDbContext();
                        var ParentOrder = await context.Orders
                                .OrderByDescending(o => o.OrderDate)
                                .FirstOrDefaultAsync(o => o.TableId == table.Id && (o.isPaid));

                        _ParentOrderID = ParentOrder?.Id;


                        var dbTable = await context.RestaurantTables.FindAsync(table.Id);
                        if (dbTable != null)
                        {
                            dbTable.Status = TABLE_FREE;
                            await context.SaveChangesAsync();
                        }


                        table.Status = TABLE_FREE;
                    }
                }
                int orderId;

                using (var context = new VinceSweetsDbContext())
                using (var tx = await context.Database.BeginTransactionAsync())
                {

                    var existingOrder = await context.Orders
                        .Where(o => o.TableId == table.Id && !o.isPaid && !o.isServed && !o.isDeleted)
                        .OrderByDescending(o => o.OrderDate)
                        .FirstOrDefaultAsync();

                    if (existingOrder != null)
                    {
                        orderId = existingOrder.Id;
                        _ParentOrderID = existingOrder.ParentOrderId;
                        await tx.CommitAsync();
                    }
                    else
                    {

                        var newOrder = new Order
                        {
                            OrderNumber = await GenerateDailyOrderNumber(context),
                            TableId = table.Id,
                            isPaid = false,
                            isServed = false,
                            isReady = false,
                            isSentToKitchen = false,
                            isDone = false,
                            OrderDate = DateTime.Now,
                            TotalAmount = 0,
                            ParentOrderId = _ParentOrderID
                        };
                        context.Orders.Add(newOrder);
                        await context.SaveChangesAsync();
                        await tx.CommitAsync();

                        _ParentOrderID = null;
                        orderId = newOrder.Id;
                    }
                }

                await OpenCashierWindow(orderId, table.Id, table.TableName, _ParentOrderID);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "error in tableswindow tableclick()");
                ToastControl.Show("خطأ", "حدث خطأ في البرنامج", ToastControl.NotificationType.Error);
            }
            finally
            {
                _ParentOrderID = null;
                if (btn != null) btn.IsEnabled = true;
            }
        }

        private async void TakeawayButton_Click(object sender, RoutedEventArgs e)
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
                        isDone = false,
                        ParentOrderId = null
                    };

                    context.Orders.Add(newOrder);
                    await context.SaveChangesAsync();
                    await tx.CommitAsync();

                    orderId = newOrder.Id;
                }

                await OpenCashierWindow(orderId, null, null, null);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "error at Takeaway in tableswindow");
                ToastControl.Show("خطأ", "تعذر إنشاء طلب سفري.", ToastControl.NotificationType.Error);

            }
            finally
            {
                if (btn != null) btn.IsEnabled = true;
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadTables();
        }
        private bool _isRealExit = false;
        private async void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            // 1. طلب التأكيد
            if (await MyConfirmDialog.ShowAsync("خروج", "هل تود اغلاق البرنامج؟"))
            {
                try
                {
                    // ✅ التحقق الذكي: هل توجد نسخة تلقائية لهذا اليوم؟
                    string docsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    string backupFolder = System.IO.Path.Combine(docsPath, "VinceApp_AutoBackups");
                    string todayPattern = $"AutoBackup_{DateTime.Now:yyyyMMdd}_*.bak";

                    // إذا المجلد موجود + فيه ملف بنفس تاريخ اليوم = لا داعي للنسخ
                    if (System.IO.Directory.Exists(backupFolder) &&
                        System.IO.Directory.GetFiles(backupFolder, todayPattern).Any())
                    {
                        // إغلاق فوري
                        _isRealExit = true;
                        Application.Current.Shutdown();
                        return;
                    }


                    ShowLoading(true, "جاري النسخ الاحتياطي والرفع السحابي ...");

                    var scheduler = new VinceApp.Services.AutoBackupScheduler();
                    await scheduler.PerformAutoBackup();
                }
                catch (Exception ex)
                {
                    Serilog.Log.Error(ex, "فشل النسخ عند الإغلاق");
                }
                finally
                {
                    ShowLoading(false);
                    _isRealExit = true;
                    Application.Current.Shutdown();
                }
            }
        }
        private void OpenAdmin_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentUser.Role == (int)UserRole.Cashier)
            {
                ToastControl.Show("صلاحيات", "هذه الصفحه مخصصة للمدراء فقط!", ToastControl.NotificationType.Info);
                return;
            }

            AdminWindow admin = new AdminWindow();
            admin.Owner = this;
            admin.ShowDialog();
            ApplyPermissions();
        }
       private void Toters_click(object sender, RoutedEventArgs e)
        {
            TotersWindow toters = new TotersWindow();
            toters.Owner = this;
            toters.ShowDialog();
        }
        private async Task OpenCashierWindow(int orderId, int? tableId, string? tableName, int? parentOrderId = null)
        {
            MainWindow cashier = null;

            try
            {
                cashier = new MainWindow(orderId, tableId, tableName, parentOrderId)
                {
                    Owner = this
                };
                cashier.ShowDialog();

                if (cashier.wasPaid)
                {
                    ToastControl.Show("تم الدفع", "تم الدفع بنجاح", ToastControl.NotificationType.Success);
                }
            }
            finally
            {
                cashier = null;

                
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                await Task.Delay(100);
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
                ToastControl.Show("خطأ", "حدث خطأ أثناء تحميل البيانات.", ToastControl.NotificationType.Error);

            }
            finally
            {
                ShowLoading(false);
                
            }

            AutoUpdater.RunUpdateAsAdmin = true;
            AutoUpdater.Start("https://raw.githubusercontent.com/SajjadAliDev12/VeniceApp/refs/heads/main/update.xml");
        }


        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingPage = new SettingsPage();

            // ربط حدث التنبيهات
            settingPage.OnNotificationReqested += (title, body) =>
            {
                ToastControl.Show(title, body, ToastControl.NotificationType.Success);
            };

            Window window = new Window
            {
                Title = "الإعدادات",
                Content = settingPage,
                Height = 750,
                Width = 800,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                FlowDirection = FlowDirection.RightToLeft,
                Owner = this,


                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent
            };

            window.ShowDialog();
        }


    }
}