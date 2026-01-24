using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading; // للمؤقت

namespace VinceKitchen
{
    public partial class DigitalClock : UserControl
    {
        private DispatcherTimer timer;

        public DigitalClock()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateTime();

            var blink = (Storyboard)Resources["BlinkColon"];
            blink.Begin(this);

            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timer.Tick += (s, args) => UpdateTime();
            timer.Start();
        }

        private void UpdateTime()
        {
            txtHour.Text = DateTime.Now.ToString("mm");
            txtMinute.Text = DateTime.Now.ToString("hh");
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            UpdateTime();
        }

        public void StopClock()
        {
            if (timer != null) timer.Stop();
        }
    }
}