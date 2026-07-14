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

namespace VinceApp.Toters_Feature
{
    /// <summary>
    /// Interaction logic for TotersOrderNumberInput.xaml
    /// </summary>
    public partial class TotersOrderNumberInput : Window
    {
        public string TotersOrderId { get;private set; }
        public TotersOrderNumberInput()
        {
            InitializeComponent();
        }
        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtTotersOrderId.Text))
            {
                TotersOrderId = txtTotersOrderId.Text;
                DialogResult = true;
            }
            else 
            {
                ToastControl.Show("خطأ","رقم الطلب غير صحيح او مفقود",ToastControl.NotificationType.Error);
                DialogResult = false;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
