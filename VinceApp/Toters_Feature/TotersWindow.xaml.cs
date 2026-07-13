using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using VinceApp.Data.Models;
using Serilog;

namespace VinceApp.Toters_Feature
{
    /// <summary>
    /// Interaction logic for TotersWindow.xaml
    /// </summary>
    public partial class TotersWindow : Window
    {
        private int? _ParentOrderID = null;
        private bool _isLoading;
        public TotersWindow()
        {
            InitializeComponent();
            LoadData();
        }
        private async void btnNewTotersOrder_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn != null) btn.IsEnabled = false;
            try
            { 
                int orderId;
                await Task.Yield();
                using (var context = new VinceSweetsDbContext())
                using (var tx = await context.Database.BeginTransactionAsync())
                {
                    var newOrder = new Order
                    {
                        OrderNumber = await GenerateDailyOrderNumber(context),
                        TableId = null,
                        isPaid = false,          // 🟢 تم التصحيح: ليس مدفوعاً بعد
                        isSentToKitchen = false,
                        isReady = false,         // 🟢 تم التصحيح: ليس جاهزاً بعد
                        isServed = false,        // 🟢 تم التصحيح: لم يسلم بعد
                        OrderDate = DateTime.Now,
                        TotalAmount = 0,
                        isDone = false,
                        ParentOrderId = null,
                        OrderSource = Data.Enums.Enums.OrderSource.EnToters,
                        PaymentMethod = Data.Enums.Enums.PaymentMethod.EnPost
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
                Log.Error(ex, "error at toters new order");
                ToastControl.Show("خطأ", "تعذر إنشاء الطلب.", ToastControl.NotificationType.Error);

            }
            finally
            {
                if (btn != null) btn.IsEnabled = true;
            }
        }
        private async Task OpenCashierWindow(int orderId, int? tableId, string? tableName, int? parentOrderId = null)
        {
            MainWindow Toters = null;

            try
            {
                Toters = new MainWindow(orderId, tableId, tableName, parentOrderId,true)
                {
                    Owner = this
                };
                Toters.ShowDialog();

                if (Toters.wasPaid)
                {
                    ToastControl.Show("تم الدفع", "تم الدفع بنجاح", ToastControl.NotificationType.Success);
                }
            }
            finally
            {
                Toters = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                LoadData();
            }
        }
        private async Task<int> GenerateDailyOrderNumber(VinceSweetsDbContext context)
        {
            var count = await context.Orders.CountAsync(o =>
                o.OrderDate.HasValue &&
                o.OrderDate.Value.Date == DateTime.Now.Date);
            return count + 1;
        }
        private void Back_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private async Task LoadActiveOrders()
        {
            using (var context = new VinceSweetsDbContext())
            {
                ActiveTotersOrdersPanel.Children.Clear();
                var activeOrders = await context.Orders
                    .Where(o => o.OrderSource == Data.Enums.Enums.OrderSource.EnToters
                             && o.TableId == null
                             && !o.isDeleted
                             && !o.isDone
                             && (o.isPaid || !o.isPaid) &&  o.OrderDate >= DateTime.Now.AddDays(-2))
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();

                foreach (var order in activeOrders)
                {
                    var cardButton = CreateOrderCardButton(order);
                    ActiveTotersOrdersPanel.Children.Add(cardButton);
                }
            }
        }

        private async Task LoadCompletedOrders()
        {
            using (var context = new VinceSweetsDbContext())
            {
                CompletedTotersOrdersPanel.Children.Clear();
                var completedOrders = await context.Orders
                    .Where(o => o.OrderSource == Data.Enums.Enums.OrderSource.EnToters
                             && o.TableId == null
                             && !o.isDeleted
                             && o.isDone
                             && o.isPaid && o.OrderDate >= DateTime.Now.AddDays(-2))
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();

                foreach (var order in completedOrders)
                {
                    var cardButton = CreateOrderCardButton(order);
                    CompletedTotersOrdersPanel.Children.Add(cardButton);
                }
            }
        }

        private async Task LoadCanceledOrders()
        {
            using (var context = new VinceSweetsDbContext())
            {
                CanceledTotersOrdersPanel.Children.Clear();
                var canceledOrders = await context.Orders
                    .Where(o => o.OrderSource == Data.Enums.Enums.OrderSource.EnToters
                             && o.TableId == null
                             && o.isDeleted && o.OrderDate >= DateTime.Now.AddDays(-2))
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();

                foreach (var order in canceledOrders)
                {
                    var cardButton = CreateOrderCardButton(order);
                    CanceledTotersOrdersPanel.Children.Add(cardButton);
                }
            }
        }
        private Button CreateOrderCardButton(Order order)
        {
            // 1. بناء الشبكة الداخلية لتوزيع النصوص هندسياً
            var cardGrid = new Grid { HorizontalAlignment = HorizontalAlignment.Stretch };
            cardGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // السطر 1: الرقم والوقت
            cardGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // السطر 2: المبلغ الصافي

            // السطر الأول: رقم الطلب والوقت
            var headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var txtOrderNum = new TextBlock
            {
                Text = $"طلب #{order.OrderNumber}",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = (Brush)FindResource("TitleBrush"),
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Grid.SetColumn(txtOrderNum, 0);

            var txtTime = new TextBlock
            {
                Text = order.OrderDate?.ToString("hh:mm tt") ?? "",
                FontSize = 14,
                Foreground = Brushes.Gray,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            Grid.SetColumn(txtTime, 1);

            headerGrid.Children.Add(txtOrderNum);
            headerGrid.Children.Add(txtTime);
            Grid.SetRow(headerGrid, 0);

            // السطر الثاني: إجمالي مبلغ الفاتورة الصافي
            var txtAmount = new TextBlock
            {
                Text = $"المبلغ: {order.TotalAmount:N0} د.ع",
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2196F3")),
                Margin = new Thickness(0, 10, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Grid.SetRow(txtAmount, 1);

            cardGrid.Children.Add(headerGrid);
            cardGrid.Children.Add(txtAmount);

            // 2. إنشاء الحاوية الرئيسية (Border) لضبط الخلفية البيضاء والانحناءات
            var cardBorder = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(12),
                BorderBrush = (Brush)FindResource("BorderBrushSoft"),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(14),
                Child = cardGrid
            };

            // 3. تحويل الكارت بأكمله إلى زر تفاعلي (Button)
            var mainButton = new Button
            {
                Content = cardBorder,
                Margin = new Thickness(0, 0, 0, 10),
                Padding = new Thickness(0),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = (VerticalAlignment)HorizontalAlignment.Stretch,
                Cursor = Cursors.Hand,
                Tag = order.Id // تخزين الـ ID الخاص بالقاعدة داخل الـ Tag لجلبه عند الضغط
            };

            // ربط حدث الضغط
            mainButton.Click += OrderCardButton_Click;

            return mainButton;
        }
        private void OrderCardButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button clickedButton && clickedButton.Tag is int orderId)
            {
                ActiveOrderStatus os =  new ActiveOrderStatus(orderId);
                os.ShowDialog();
                LoadData();
            }
        }
        private async void LoadData()
        {
            if (_isLoading) return;
            _isLoading = true;
            await LoadActiveOrders();
            await LoadCompletedOrders();
            await LoadCanceledOrders();
            _isLoading = false;
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }
    }
}
