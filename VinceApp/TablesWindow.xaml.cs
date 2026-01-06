using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VinceApp.Data;
using Microsoft.EntityFrameworkCore;


namespace VinceApp
{
    public partial class TablesWindow : Window
    {
        private const int TABLE_FREE = 0;
        private const int TABLE_BUSY = 1;
        private const int TABLE_PAID = 2;

        public TablesWindow()
        {
            InitializeComponent();
            _ = LoadTables();
        }

        // ============================
        // رسم الطاولات + السفري (Async)
        // ============================
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

                    // --- رسم الطاولات (لم يتغير) ---
                    if (!await context.RestaurantTables.AnyAsync())
                    {
                        for (int i = 1; i <= 12; i++)
                            context.RestaurantTables.Add(new RestaurantTable { TableNumber = i, Status = TABLE_FREE });
                        await context.SaveChangesAsync();
                    }

                    var tables = await context.RestaurantTables.AsNoTracking().OrderBy(t => t.TableNumber).ToListAsync();

                    foreach (var table in tables)
                    {
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
                    // التعديل هنا: منطق الطلبات السفرية (Open + Paid)
                    // =========================================================
                    if (TakeawayPanel != null)
                    {
                        // نجلب الطلبات المفتوحة والمدفوعة (التي لم تسلم بعد)
                        var takeawayOrders = await context.Orders
                            .Where(o => o.TableId == null && (o.OrderStatus == "Open" || o.OrderStatus == "Paid"))
                            .OrderByDescending(o => o.OrderDate)
                            .ToListAsync();

                        foreach (var order in takeawayOrders)
                        {
                            bool isPaid = order.OrderStatus == "Paid";

                            Button btnTakeaway = new Button
                            {
                                Height = 80,
                                Margin = new Thickness(0, 5, 0, 5),
                                Foreground = Brushes.White,
                                Tag = order.Id,
                                ToolTip = "اضغط بالزر الأيسر للعرض، وبالزر الأيمن للخيارات"
                            };

                            // تنسيق الألوان حسب الحالة
                            if (isPaid)
                            {
                                // أخضر غامق للمدفوع
                                btnTakeaway.Background = new SolidColorBrush(Color.FromRgb(46, 125, 50));
                                btnTakeaway.BorderBrush = Brushes.LightGreen;
                            }
                            else
                            {
                                // بنفسجي للمفتوح
                                btnTakeaway.Background = new SolidColorBrush(Color.FromRgb(106, 27, 154));
                                btnTakeaway.BorderBrush = Brushes.Purple;
                            }
                            btnTakeaway.BorderThickness = new Thickness(2);

                            // النص داخل الزر
                            string statusText = isPaid ? "✅ (مدفوع)" : "⏳ (انتظار)";
                            string time = order.OrderDate?.ToString("hh:mm tt") ?? "";

                            var stack = new StackPanel { Orientation = Orientation.Vertical };
                            stack.Children.Add(new TextBlock { Text = $"📦 طلب #{order.OrderNumber} {statusText}", FontWeight = FontWeights.Bold, FontSize = 16, HorizontalAlignment = HorizontalAlignment.Center });
                            stack.Children.Add(new TextBlock { Text = $"{time}", FontSize = 12, Foreground = Brushes.LightGray, HorizontalAlignment = HorizontalAlignment.Center });
                            btnTakeaway.Content = stack;

                            // 1. الزر الأيسر: فتح الفاتورة (للتعديل أو العرض)
                            btnTakeaway.Click += async (s, e) =>
                            {
                                if (s is Button b && b.Tag is int orderId)
                                {
                                    if (isPaid)
                                    {
                                        // === إذا كان مدفوعاً: نفتح نافذة الخيارات (لمس) ===
                                        var dialog = new TakeawayOptionsWindow();
                                        dialog.ShowDialog(); // ننتظر اختيار المستخدم

                                        if (dialog.UserChoice == "Serve")
                                        {
                                            // اختار تسليم -> نخفي الطلب
                                            await CompleteOrderAsync(orderId);
                                        }
                                        else if (dialog.UserChoice == "View")
                                        {
                                            // اختار عرض -> نفتح الفاتورة للقراءة فقط
                                            OpenCashierWindow(orderId, null);
                                        }
                                        // إذا اختار Cancel لا نفعل شيئاً
                                    }
                                    else
                                    {
                                        // === إذا كان غير مدفوع: نفتح الكاشير مباشرة للدفع ===
                                        OpenCashierWindow(orderId, null);
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
                MessageBox.Show($"خطأ: {ex.Message}");
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
                            order.OrderStatus = "Completed"; // حالة جديدة تعني انتهى
                            await context.SaveChangesAsync();
                        }
                    }
                    await LoadTables(); // تحديث الشاشة
                }
                catch { MessageBox.Show("فشل التحديث"); }
            }
        }

        // دالة إلغاء الطلب
        private async Task CancelOrderAsync(int orderId)
        {
            if (MessageBox.Show("هل أنت متأكد من إلغاء وحذف هذا الطلب؟", "تحذير", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    using (var context = new VinceSweetsDbContext())
                    {
                        var order = await context.Orders.FindAsync(orderId);
                        if (order != null)
                        {
                            context.Orders.Remove(order); // حذف نهائي
                            await context.SaveChangesAsync();
                        }
                    }
                    await LoadTables();
                }
                catch { MessageBox.Show("فشل الحذف"); }
            }
        }
        // ============================
        // حدث الضغط على الطاولة
        // ============================
        private async void Table_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var table = btn?.Tag as RestaurantTable;
            if (table == null) return;

            btn.IsEnabled = false;

            try
            {
                if (table.Status == TABLE_PAID)
                {
                    var dialog = new TableActionWindow();
                    dialog.ShowDialog();

                    if (dialog.UserChoice == "Cancel")
                    {
                        btn.IsEnabled = true;
                        return;
                    }

                    if (dialog.UserChoice == "Clear")
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

                    if (dialog.UserChoice == "NewOrder")
                    {
                        table.Status = TABLE_FREE;
                    }
                }

                int orderId;

                using (var context = new VinceSweetsDbContext())
                {
                    using (var tx = await context.Database.BeginTransactionAsync())
                    {
                        if (table.Status == TABLE_FREE)
                        {
                            var newOrder = new Order
                            {
                                OrderNumber = await GenerateDailyOrderNumber(context),
                                TableId = table.Id,
                                OrderStatus = "Open",
                                OrderDate = DateTime.Now,
                                TotalAmount = 0
                            };

                            context.Orders.Add(newOrder);

                            var dbTable = await context.RestaurantTables.FindAsync(table.Id);
                            if (dbTable != null) dbTable.Status = TABLE_BUSY;

                            await context.SaveChangesAsync();
                            await tx.CommitAsync();

                            orderId = newOrder.Id;
                        }
                        else
                        {
                            var existingOrder = await context.Orders.FirstOrDefaultAsync(o =>
                                o.TableId == table.Id && o.OrderStatus == "Open");

                            if (existingOrder == null)
                            {
                                var dbTable = await context.RestaurantTables.FindAsync(table.Id);
                                if (dbTable != null)
                                {
                                    dbTable.Status = TABLE_FREE;
                                    await context.SaveChangesAsync();
                                }
                                await LoadTables();
                                btn.IsEnabled = true;
                                return;
                            }
                            orderId = existingOrder.Id;
                        }
                    }
                }

                OpenCashierWindow(orderId, table.Id);
            }
            catch (Exception ex)
            {
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
        private async void TakeawayButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn != null) btn.IsEnabled = false;

            try
            {
                int orderId;

                using (var context = new VinceSweetsDbContext())
                using (var tx = await context.Database.BeginTransactionAsync())
                {
                    var newOrder = new Order
                    {
                        OrderNumber = await GenerateDailyOrderNumber(context),
                        TableId = null, // سفري
                        OrderStatus = "Open",
                        OrderDate = DateTime.Now,
                        TotalAmount = 0
                    };

                    context.Orders.Add(newOrder);
                    await context.SaveChangesAsync();
                    await tx.CommitAsync();

                    orderId = newOrder.Id;
                }

                OpenCashierWindow(orderId, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"تعذر إنشاء طلب سفري.\n{ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
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
            Application.Current.Shutdown();
        }

        // ============================
        // أدوات مساعدة
        // ============================
        private void OpenCashierWindow(int orderId, int? tableId)
        {
            MainWindow cashier = new MainWindow(orderId, tableId);
            this.Hide();
            cashier.ShowDialog();
            this.Show();
            _ = LoadTables(); // تحديث بعد العودة
        }

        private async Task<int> GenerateDailyOrderNumber(VinceSweetsDbContext context)
        {
            var count = await context.Orders.CountAsync(o =>
                o.OrderDate.HasValue &&
                o.OrderDate.Value.Date == DateTime.Now.Date);
            return count + 1;
        }
    }
}