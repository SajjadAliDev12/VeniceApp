using System.Windows;
using VinceApp.Data;
using VinceApp.Data.Models;
using VinceApp.Pages; // سنحتاجه لاحقاً

namespace VinceApp
{
    public partial class AdminWindow : Window
    {
        public AdminWindow()
        {
            InitializeComponent();
            ApplyPermissions();
            MainFrame.Navigate(new DashboardPage());
        }
        private void ApplyPermissions()
        {
            if (CurrentUser.Role == (int)UserRole.Manager)
            {
                BtnUsers.IsEnabled = false;
                BtnSales.IsEnabled = false;
                BtnAudit.IsEnabled = false;
            }
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
        private void Nav_Audit_Click(object sender, RoutedEventArgs e)
        {
            PageTitle.Text = "سجل حركات النظام (Audit Log)";
            MainFrame.Navigate(new VinceApp.Pages.AuditLogPage());
        }
        public async Task<bool> ShowConfirmMessage(string title, string message)
        {
            // استدعاء الدالة الموجودة داخل اليوزر كونترول
            return await GlobalConfirmDialog.ShowAsync(title, message);
        }
        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            
            this.Close(); 
        }
    }
}