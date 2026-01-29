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
using VinceApp.Data;
using VinceApp.Data.Models;
using VinceApp.Pages;


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

        public TablesWindow()
        {
            InitializeComponent();
            ToastControl.Show("Welcome", $"تم تسجيل الدخول بأسم : {CurrentUser.FullName}", ToastControl.NotificationType.Success);


        }
        private async Task LoadTables()
        {
            if (_isLoading) return;
            _isLoading = true;

            TablesPanel.Children.Clear();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            if (TakeawayPanel != null) TakeawayPanel.Children.Clear();

            try
            {
                using (var context = new VinceSweetsDbContext())
                {
                    var tables = await context.RestaurantTables
                        .AsNoTracking()
                        .OrderBy(t => t.TableNumber)
                        .ToListAsync();

                    // ✅ 1) الطاولات التي عليها طلب مفتوح فعّال
                    var busyTableIds = await context.Orders
                        .Where(o => o.TableId != null && !o.isPaid && !o.isServed && !o.isDeleted)
                        .Select(o => o.TableId!.Value)
                        .Distinct()
                        .ToListAsync();

                    // ✅ 2) الطاولات التي آخر طلب لها مدفوع
                    var paidTableIds = await context.Orders
                        .Where(o => o.TableId != null && o.isPaid && !o.isDeleted && !o.isDone)
                        .GroupBy(o => o.TableId!.Value)
                        .Select(g => g.Key)
                        .ToListAsync();

                    var busySet = busyTableIds.ToHashSet();
                    var paidSet = paidTableIds.ToHashSet();

                    foreach (var table in tables)
                    {
                        int computedStatus =
                            busySet.Contains(table.Id) ? TABLE_BUSY :
                            paidSet.Contains(table.Id) ? TABLE_PAID :
                            TABLE_FREE;

                        // --- تصميم زر الطاولة الجديد ---
                        Button btnTable = new Button
                        {
                            Width = 200,    // حجم مناسب للبطاقة
                            Height = 180,
                            Margin = new Thickness(10),
                            Tag = table,
                            Cursor = Cursors.Hand,
                            Background = Brushes.White, // الخلفية بيضاء دائماً
                            BorderThickness = new Thickness(5) // سمك الإطار
                        };

                        // جعل الزوايا دائرية
                        var borderStyle = new Style(typeof(Border));
                        borderStyle.Setters.Add(new Setter(Border.CornerRadiusProperty, new CornerRadius(12)));
                        btnTable.Resources.Add(typeof(Border), borderStyle);

                        // تأثير ظل خفيف
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

                        // محتوى الزر (أيقونة + رقم + حالة)
                        var stack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                        var txtIcon = new TextBlock { FontSize = 34, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 5) };
                        var txtNumber = new TextBlock { Text = $"طاولة {table.TableNumber}", FontSize = 24, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center };
                        var txtStatus = new TextBlock { FontSize = 22, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 5, 0, 0) };

                        // --- تخصيص الألوان بناءً على الحالة ---
                        if (computedStatus == TABLE_FREE)
                        {
                            btnTable.BorderBrush = new SolidColorBrush(Color.FromRgb(76, 175, 80)); // إطار أخضر
                            txtIcon.Text = "🍽️";
                            txtStatus.Text = "(فارغة)";
                            txtStatus.Foreground = Brushes.Gray;
                            txtNumber.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50)); // نص غامق
                        }
                        else if (computedStatus == TABLE_BUSY)
                        {
                            btnTable.BorderBrush = new SolidColorBrush(Color.FromRgb(229, 57, 53)); // إطار أحمر
                            txtIcon.Text = "⛔"; // أو أيقونة مشغول
                            txtStatus.Text = "(مشغولة)";
                            txtStatus.Foreground = new SolidColorBrush(Color.FromRgb(229, 57, 53));
                            txtNumber.Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40));
                        }
                        else // TABLE_PAID
                        {
                            btnTable.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 167, 38)); // إطار برتقالي
                            txtIcon.Text = "💰";
                            txtStatus.Text = "(مدفوع)";
                            txtStatus.Foreground = new SolidColorBrush(Color.FromRgb(239, 108, 0));
                            txtNumber.Foreground = new SolidColorBrush(Color.FromRgb(239, 108, 0));
                        }

                        stack.Children.Add(txtIcon);
                        stack.Children.Add(txtNumber);
                        stack.Children.Add(txtStatus);
                        btnTable.Content = stack;

                        TablesPanel.Children.Add(btnTable);
                    }

                    // --- قسم الطلبات السفري ---
                    if (TakeawayPanel != null)
                    {
                        var takeawayOrders = await context.Orders
                            .Where(o => o.TableId == null && !o.isServed && !o.isDone && !o.isDeleted)
                            .OrderByDescending(o => o.OrderDate)
                            .ToListAsync();

                        foreach (var order in takeawayOrders)
                        {
                            bool isPaid = order.isPaid;
                            bool isReady = order.isReady;

                            Button btnTakeaway = new Button
                            {
                                Height = 70, // ارتفاع أقل قليلاً لشكل أنيق
                                Margin = new Thickness(0, 0, 0, 10),
                                Tag = order.Id,
                                Cursor = Cursors.Hand,
                                Background = Brushes.White, // بطاقة بيضاء
                                BorderThickness = new Thickness(0, 0, 8, 0), // خط ملون جانبي فقط
                                HorizontalContentAlignment = HorizontalAlignment.Stretch
                            };

                            // تدوير الزوايا للزر
                            var takeawayBorderStyle = new Style(typeof(Border));
                            takeawayBorderStyle.Setters.Add(new Setter(Border.CornerRadiusProperty, new CornerRadius(8)));
                            btnTakeaway.Resources.Add(typeof(Border), takeawayBorderStyle);

                            string statusIcon = "";
                            string statusText = "";
                            SolidColorBrush statusColor;

                            if (isPaid)
                            {
                                statusColor = new SolidColorBrush(Color.FromRgb(67, 160, 71)); // أخضر
                                btnTakeaway.BorderBrush = statusColor;
                                statusIcon = "✅";
                                statusText = "مدفوع";
                            }
                            else if (isReady)
                            {
                                statusColor = new SolidColorBrush(Color.FromRgb(251, 140, 0)); // برتقالي
                                btnTakeaway.BorderBrush = statusColor;
                                statusIcon = "🔔";
                                statusText = "جاهز";
                            }
                            else
                            {
                                statusColor = new SolidColorBrush(Color.FromRgb(142, 36, 170)); // بنفسجي
                                btnTakeaway.BorderBrush = statusColor;
                                statusIcon = "⏳";
                                statusText = "تحضير";
                            }

                            string time = order.OrderDate?.ToString("hh:mm tt") ?? "";

                            // تصميم محتوى زر السفري
                            var grid = new Grid();
                            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                            // الجهة اليمنى: الرقم والحالة
                            var infoStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                            infoStack.Children.Add(new TextBlock { Text = $"#{order.OrderNumber} {statusText}", FontWeight = FontWeights.Bold, FontSize = 24, Foreground = Brushes.Black, Margin = new Thickness(0, 0, 10, 0) });
                            infoStack.Children.Add(new TextBlock { Text = statusIcon, FontSize = 20, VerticalAlignment = VerticalAlignment.Center });

                            // الجهة اليسرى: الوقت
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
                            order.isServed = true;
                            order.isDone = true;
                            await context.SaveChangesAsync();
                        }
                    }
                    await LoadTables(); ToastControl.Show("تم اكمال الطلب", "تم اخفاء الطلب من القائمة ", ToastControl.NotificationType.Info);
                }
                catch(Exception ex) {
                    ToastControl.Show( "فشل","فشل التحديث",ToastControl.NotificationType.Error);  Log.Error(ex, "error with CompleteOrderAsync in tableswindow");
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
                                // نفتح النافذة (وبما أن الحالة Paid، ستفتح للقراءة فقط تلقائياً)
                                OpenCashierWindow(paidOrder.Id, table.Id,table.TableName,null);
                            }
                            else
                            {
                                ToastControl.Show("خطأ", "لم يتم العثور على القائمة المدفوعه", ToastControl.NotificationType.Error);
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

                OpenCashierWindow(orderId, table.Id, table.TableName, null);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "error in tableswindow tableclick()");
                ToastControl.Show("خطأ", "حدث خطأ في البرنامج", ToastControl.NotificationType.Error);
            }
            finally
            {
                if (btn != null) btn.IsEnabled = true;
            }
        }

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
                        isDone = false,
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
                ToastControl.Show( "خطأ","تعذر إنشاء طلب سفري.", ToastControl.NotificationType.Error);
                
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

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
        private void OpenAdmin_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentUser.Role == (int)UserRole.Cashier)
                {ToastControl.Show("صلاحيات","هذه الصفحه مخصصة للمدراء فقط!",ToastControl.NotificationType.Info); 
                return; }

            AdminWindow admin = new AdminWindow();
            admin.Owner = this;
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

            AutoUpdater.RunUpdateAsAdmin = true;
            AutoUpdater.Start("https://raw.githubusercontent.com/SajjadAliDev12/VeniceApp/refs/heads/main/update.xml");
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
        }


        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingPage = new SettingsPage();

            // ربط حدث التنبيهات
            settingPage.OnNotificationReqested += (title, body) =>
            {
                ToastControl.Show(title, body, ToastControl.NotificationType.Info);
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