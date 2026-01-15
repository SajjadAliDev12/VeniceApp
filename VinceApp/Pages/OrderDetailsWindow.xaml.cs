using System.Collections.Generic; // مهمة للقوائم
using System.Drawing.Printing;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using VinceApp.Data;
using VinceApp.Services;

namespace VinceApp
{
    public partial class OrderDetailsWindow : Window
    {
        private int _orderId;
        public OrderDetailsWindow(int orderId)
        {
            InitializeComponent();
            LoadDetails(orderId);
            _orderId = orderId;
        }

        private void LoadDetails(int orderId)
        {
            using (var context = new VinceSweetsDbContext())
            {
                var order = context.Orders.Find(orderId);
                if (order != null)
                {
                    txtTitle.Text = $"فاتورة رقم #{order.OrderNumber}";
                    txtDate.Text = $"التاريخ: {order.OrderDate:yyyy/MM/dd hh:mm tt}";

                    // تحديد نوع الطلب (صالة / سفري)
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
                }

                // 1. جلب البيانات وتحويلها للكلاس المساعد
                List<InvoiceItem> itemList = context.OrderDetails
                    .Where(d => d.OrderId == orderId)
                    .Select(d => new InvoiceItem
                    {
                        ProductName = d.ProductName,
                        Price = d.Price,       // السعر
                        Quantity = d.Quantity, // الكمية
                        Total = d.Price * d.Quantity,
                        IsTotalRow = false     // هذا سطر عادي
                    })
                    .ToList();

                // 2. حساب المجموع الكلي
                decimal grandTotal = itemList.Sum(x => x.Total);

                // 3. إضافة سطر "المجموع الكلي" للقائمة
                itemList.Add(new InvoiceItem
                {
                    ProductName = "=== المجموع الكلي ===",
                    Price = null,    // نتركه فارغاً ليظهر فراغ في الجدول
                    Quantity = null, // نتركه فارغاً
                    Total = grandTotal,
                    IsTotalRow = true // نميزه لتلوينه لاحقاً
                });

                // 4. ربط القائمة بالجدول
                dgDetails.ItemsSource = itemList;
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            TicketPrinter ticketPrinter = new TicketPrinter();
            ticketPrinter.Print(_orderId, false);
        }
    }

    // كلاس مساعد لترتيب البيانات داخل الجدول
    public class InvoiceItem
    {
        public string ProductName { get; set; }
        public decimal? Price { get; set; }    // جعلناه Nullable ليقبل الفراغ في سطر المجموع
        public int? Quantity { get; set; }     // جعلناه Nullable
        public decimal Total { get; set; }
        public bool IsTotalRow { get; set; }   // للتمييز بين المنتج والمجموع
    }
}