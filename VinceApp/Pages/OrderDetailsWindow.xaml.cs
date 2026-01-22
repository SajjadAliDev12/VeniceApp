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

                    // نوع الطلب
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

                    // جلب التفاصيل
                    var query = context.OrderDetails.Where(d => d.OrderId == orderId);
                    if (!order.isDeleted)
                    {
                        query = query.Where(d => d.isDeleted == false);
                    }

                    List<InvoiceItem> itemList = query
                        .Select(d => new InvoiceItem
                        {
                            ProductName = d.ProductName,
                            Price = d.Price,
                            Quantity = d.Quantity,
                            Total = d.Price * d.Quantity,
                            IsTotalRow = false,
                            RowColor = "Black" // اللون الافتراضي للنصوص العادية
                        })
                        .ToList();

                    // === التعديل الجوهري هنا: الحسابات ===

                    decimal subTotal = itemList.Sum(x => x.Total);
                    decimal discount = order.DiscountAmount; // جلب الخصم
                    decimal finalTotal = subTotal - discount;     // الصافي

                    // 1. إضافة سطر المجموع الفرعي (إذا كان هناك خصم فقط)
                    if (discount > 0)
                    {
                        itemList.Add(new InvoiceItem
                        {
                            ProductName = "المجموع الفرعي",
                            Total = subTotal,
                            IsTotalRow = true,
                            RowColor = "Gray"
                        });

                        // 2. إضافة سطر الخصم
                        itemList.Add(new InvoiceItem
                        {
                            ProductName = "قيمة الخصم",
                            Total = -discount, // بالسالب للتوضيح
                            IsTotalRow = true,
                            RowColor = "#D32F2F" // أحمر
                        });
                    }

                    // 3. إضافة سطر الصافي النهائي
                    itemList.Add(new InvoiceItem
                    {
                        ProductName = (discount > 0) ? "الصافي النهائي" : "المجموع الكلي",
                        Total = finalTotal,
                        IsTotalRow = true,
                        RowColor = "#00695C" // أخضر
                    });

                    dgDetails.ItemsSource = itemList;
                }
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
        public decimal? Price { get; set; }
        public int? Quantity { get; set; }
        public decimal Total { get; set; }
        public bool IsTotalRow { get; set; }

        // خاصية جديدة للتحكم بلون السطر (أحمر للخصم، أخضر للمجموع)
        public string RowColor { get; set; }
    }
}