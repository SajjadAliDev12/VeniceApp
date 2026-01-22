using System.Windows;

namespace VinceApp
{
    public partial class TableActionWindow : Window
    {
        // "View" = عرض الفاتورة
        // "NewOrder" = طلب جديد
        // "Clear" = إخلاء
        // "Cancel" = إلغاء
        public string UserChoice { get; private set; } = "Cancel";

        public TableActionWindow()
        {
            InitializeComponent();
        }

        private void ViewBill_Click(object sender, RoutedEventArgs e)
        {
            UserChoice = "View";
            this.Close();
        }

        private void NewOrder_Click(object sender, RoutedEventArgs e)
        {
            UserChoice = "NewOrder";
            this.Close();
        }

        private void ClearTable_Click(object sender, RoutedEventArgs e)
        {
            UserChoice = "Clear";
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            UserChoice = "Cancel";
            this.Close();
        }
    }
}