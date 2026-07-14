using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using VinceApp.Data.Models;
using VinceApp.Services;

namespace VinceApp
{
    public partial class OrderDetailsWindow : Window
    {
        private int _orderId;

        public OrderDetailsWindow(int orderId)
        {
            InitializeComponent();
            _orderId = orderId;

            // ✅ الحل الصحيح: استدعاء الدالة عند تحميل النافذة على الـ UI Thread
            this.Loaded += async (s, e) => await LoadDetailsAsync(_orderId);
        }

        // ✅ تم تحويل الدالة إلى Task لتكون غير متزامنة بشكل صحيح
        private async Task LoadDetailsAsync(int orderId)
        {
            try
            {
                using (var context = new VinceSweetsDbContext())
                {
                    // ✅ استخدام FindAsync و AsNoTracking لتسريع الأداء (لأننا للعرض فقط)
                    var order = await context.Orders.FindAsync(orderId);

                    if (order != null)
                    {
                        txtTitle.Text = $"فاتورة رقم #{order.Id}";
                        txtDate.Text = $"التاريخ: {order.OrderDate:yyyy/MM/dd hh:mm tt}";

                        // معالجة حالة الإلغاء
                        if (order.isDeleted)
                        {
                            txtTitle.Text += " (ملغاة ❌)";
                            txtTitle.Foreground = Brushes.Red;
                        }
                        else
                        {
                            txtTitle.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3E2723"));
                        }

                        // نوع الطلب (صالة / سفري)
                        if (order.TableId != null)
                        {
                            txtOrderType.Text = "🍽️ طلب صالة";
                            borderType.Background = new SolidColorBrush(Color.FromRgb(255, 243, 224));
                            txtOrderType.Foreground = new SolidColorBrush(Color.FromRgb(230, 81, 0));
                        }
                        else
                        {
                            txtOrderType.Text = "🛍️ سفري (Takeaway)";
                            borderType.Background = new SolidColorBrush(Color.FromRgb(225, 245, 254));
                            txtOrderType.Foreground = new SolidColorBrush(Color.FromRgb(1, 87, 155));
                        }

                        // التحقق مما إذا كان الطلب ملحقاً
                        if (order.ParentOrderId != null)
                        {
                            var parentOrderNum = await context.Orders
                                .Where(o => o.Id == order.ParentOrderId)
                                .Select(o => o.Id)
                                .FirstOrDefaultAsync();

                            if (parentOrderNum > 0)
                            {
                                txtOrderType.Text += $" - (ملحق للطلب #{parentOrderNum})";
                            }
                        }

                        // جلب التفاصيل
                        var query = context.OrderDetails
                            .AsNoTracking()
                            .Where(d => d.OrderId == orderId);

                        if (!order.isDeleted)
                        {
                            query = query.Where(d => d.isDeleted == false);
                        }

                        List<InvoiceItem> itemList = await query
                            .Select(d => new InvoiceItem
                            {
                                ProductName = d.ProductName,
                                Price = d.Price,
                                Quantity = d.Quantity,
                                Total = d.Price * d.Quantity,
                                IsTotalRow = false,
                                RowColor = "Black"
                            })
                            .ToListAsync();

                        // === الحسابات ===
                        decimal? subTotal = itemList.Sum(x => x.Total);
                        decimal? discount = order.DiscountAmount;
                        decimal? finalTotal = subTotal - discount;

                        if (discount > 0)
                        {
                            itemList.Add(new InvoiceItem
                            {
                                ProductName = "المجموع الفرعي",
                                Total = subTotal,
                                IsTotalRow = true,
                                RowColor = "Gray"
                            });

                            itemList.Add(new InvoiceItem
                            {
                                ProductName = "قيمة الخصم",
                                Total = -discount,
                                IsTotalRow = true,
                                RowColor = "#D32F2F"
                            });
                        }

                        itemList.Add(new InvoiceItem
                        {
                            ProductName = (discount > 0) ? "الصافي النهائي" : "المجموع الكلي",
                            Total = finalTotal,
                            IsTotalRow = true,
                            RowColor = "#00695C"
                        });

                        dgDetails.ItemsSource = itemList;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Serilog.Log.Error(ex, "Error loading order details");
                ToastControl.Show("خطأ", "فشل تحميل تفاصيل الطلب", ToastControl.NotificationType.Error);
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            bool printed = false;

            // ✅ إضافة using لمنع تسرب الذاكرة
            using (var ticketPrinter = new TicketPrinter())
            {  
                printed = await Task.Run(() => ticketPrinter.Print(_orderId));
            }

            if (printed)
            {
                ToastControl.Show("تمت الطباعة", "تمت طباعة الوصل بنجاح", ToastControl.NotificationType.Success);
            }
            else
            {
                ToastControl.Show("لم تتم الطباعة", "فشل طباعة الوصل !", ToastControl.NotificationType.Error);
            }
        }
    }

    public class InvoiceItem
    {
        public string ProductName { get; set; }
        public decimal? Price { get; set; }
        public int? Quantity { get; set; }
        public decimal? Total { get; set; }
        public bool IsTotalRow { get; set; }
        public string RowColor { get; set; }
    }
}