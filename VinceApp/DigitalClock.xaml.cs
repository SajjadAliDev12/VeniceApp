using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading; // للمؤقت

namespace VinceApp
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
            // إعداد المؤقت مرة واحدة
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(500);
            timer.Tick += Timer_Tick;
            timer.Start();

            // تحديث الوقت فوراً عند التحميل
            UpdateClock();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            UpdateClock();
        }

        private void UpdateClock()
        {
            DateTime now = DateTime.Now;
            txtTime.Text = now.ToString("hh:mm tt");
        }

        // تنظيف الذاكرة عند إغلاق الأداة (اختياري ولكنه جيد)
        public void StopClock()
        {
            if (timer != null) timer.Stop();
        }
    }
}