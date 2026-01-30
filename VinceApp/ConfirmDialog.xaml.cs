using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace VinceApp
{
    public partial class ConfirmDialog : UserControl
    {
    
        private TaskCompletionSource<bool> _tcs;

        public ConfirmDialog()
        {
            InitializeComponent();
            this.Visibility = Visibility.Collapsed; 
        }
        public Task<bool> ShowAsync(string title, string message)
        {
            TxtTitle.Text = title;
            TxtMessage.Text = message;

            
            _tcs = new TaskCompletionSource<bool>();

            this.Visibility = Visibility.Visible;
            var sb = this.Resources["PopInAnimation"] as Storyboard;
            sb?.Begin();

            return _tcs.Task;
        }

        private void Yes_Click(object sender, RoutedEventArgs e)
        {
            CloseDialog(true);
        }

        private void No_Click(object sender, RoutedEventArgs e)
        {
            CloseDialog(false);
        }
        private void Grid_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.OriginalSource is Grid)
            {
                CloseDialog(false);
            }
        }

        private void CloseDialog(bool result)
        {
            this.Visibility = Visibility.Collapsed;
            _tcs?.TrySetResult(result);
        }
    }
}