using System.Windows;

namespace VinceApp
{
    public partial class TakeawayOptionsWindow : Window
    {
        // خاصية لمعرفة ماذا اختار المستخدم
        public string UserChoice { get; private set; } = "Cancel";

        public TakeawayOptionsWindow()
        {
            InitializeComponent();
        }

        private void BtnServe_Click(object sender, RoutedEventArgs e)
        {
            UserChoice = "Serve";
            Close();
        }

        private void BtnView_Click(object sender, RoutedEventArgs e)
        {
            UserChoice = "View";
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            UserChoice = "Add";
            Close();
        }
    }
}