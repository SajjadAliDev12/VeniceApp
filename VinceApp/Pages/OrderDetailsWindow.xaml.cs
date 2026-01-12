using System.Linq;
using System.Windows;
using VinceApp.Data;

namespace VinceApp
{
    public partial class OrderDetailsWindow : Window
    {
        public OrderDetailsWindow(int orderId)
        {
            InitializeComponent();
            LoadDetails(orderId);
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
                }

                // جلب المنتجات وحساب المجموع لكل سطر
                var details = context.OrderDetails
                    .Where(d => d.OrderId == orderId)
                    .Select(d => new
                    {
                        d.ProductName,
                        d.Price,
                        d.Quantity,
                        Total = d.Price * d.Quantity
                    })
                    .ToList();

                dgDetails.ItemsSource = details;
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}