using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace VinceApp
{
    public partial class DigitalClock : UserControl
    {
        private DispatcherTimer timer; // المتغير العام للكلاس

        public DigitalClock()
        {
            InitializeComponent();

            // يجب ربط حدث الخروج هنا لضمان توقف الساعة عند إغلاق النافذة
            this.Unloaded += UserControl_Unloaded;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // تحديث الوقت فوراً
            UpdateTime();

            // تشغيل الأنيميشن (تأكد أن BlinkColon موجود في الـ Resources)
            if (Resources["BlinkColon"] is Storyboard blink)
            {
                blink.Begin(this);
            }

            // التصحيح هنا: حذفنا var لنستخدم المتغير العام
            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timer.Tick += (s, args) => UpdateTime();
            timer.Start();
        }

        private void UpdateTime()
        {
            txtHour.Text = DateTime.Now.ToString("hh"); // hh للساعات
            txtMinute.Text = DateTime.Now.ToString("mm"); // mm للدقائق
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            StopClock();
        }

        public void StopClock()
        {
            if (timer != null)
            {
                timer.Stop();
                timer = null; // تفريغ المتغير للسماح للـ Garbage Collector بحذفه
            }
        }
    }
}