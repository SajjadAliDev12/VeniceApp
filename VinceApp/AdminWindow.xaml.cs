using System.Windows;
using VinceApp.Pages; // سنحتاجه لاحقاً

namespace VinceApp
{
    public partial class AdminWindow : Window
    {
        public AdminWindow()
        {
            InitializeComponent();
            // عند الفتح، نذهب للرئيسية تلقائياً
            MainFrame.Navigate(new DashboardPage());
        }

        private void Nav_Dashboard_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new DashboardPage());
            PageTitle.Text = "الإحصائيات العامة";
        }

        private void Nav_Products_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new Pages.ProductsPage());
            PageTitle.Text = "إدارة المنتجات";
        }

        private void Nav_Categories_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new Pages.CategoriesPage());
            PageTitle.Text = "إدارة التصنيفات";
        }

        private void Nav_Tables_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new Pages.TablesPage());
            PageTitle.Text = "إدارة الطاولات";
        }

        private void Nav_Users_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new Pages.UsersPage());
            PageTitle.Text = "إدارة المستخدمين";
        }

        private void Nav_Reports_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new Pages.OrdersPage());
            PageTitle.Text = "أرشيف الفواتير";
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            this.Close(); // يغلق الإدارة ويعود للكاشير
        }
    }
}