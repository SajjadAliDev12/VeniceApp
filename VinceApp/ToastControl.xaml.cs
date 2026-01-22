using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using VinceApp.Pages;

namespace VinceApp
{
    public partial class ToastControl : UserControl
    {
        private DispatcherTimer _timer;

        public enum NotificationType
        {
            Success,
            Error,
            Warning,
            Info
        }

        public static void Show(string title, string message, NotificationType type)
        {
            switch (type)
            {
                case NotificationType.Success:
                    System.Media.SystemSounds.Beep.Play(); // صوت نجاح خفيف
                    break;
                case NotificationType.Error:
                    System.Media.SystemSounds.Hand.Play(); // صوت خطأ (Windows Error)
                    break;
                case NotificationType.Warning:
                    System.Media.SystemSounds.Exclamation.Play(); // صوت تنبيه
                    break;
                default:
                    System.Media.SystemSounds.Beep.Play(); // صوت عادي
                    break;
            }
                    Application.Current.Dispatcher.Invoke(() =>
            {
                // ✅ استهدف النافذة النشطة (أي شاشة مفتوحة الآن)
                Window targetWindow =
                    Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
                    ?? Application.Current.MainWindow;

                if (targetWindow == null) return;
                var container = targetWindow.FindName("NotificationArea") as Panel;

                // 3. اللحظة الحاسمة: إذا لم نجد المنطقة (مثل حالة الإعدادات)، نبدل الهدف إلى النافذة الرئيسية
                if (container == null)
                {
                    targetWindow = Application.Current.MainWindow;
                    // نبحث مرة أخرى في النافذة الرئيسية
                    container = targetWindow?.FindName("NotificationArea") as Panel;
                }

                // إذا ظل فارغاً حتى بعد المحاولة في الرئيسية، نخرج
                if (container == null) return;

                var toast = new ToastControl(title, message, type);

                // يظهر بالأحدث فوق (اختياري)
                container.Children.Insert(0, toast);
                // أو تحت:
                // container.Children.Add(toast);
            });
        }

        public ToastControl(string title, string message, NotificationType type)
        {
            InitializeComponent();

            TxtTitle.Text = title;
            TxtMessage.Text = message;

            switch (type)
            {
                case NotificationType.Success:
                    MainBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
                    IconText.Text = "✓";
                    IconText.Foreground = MainBorder.BorderBrush;
                    break;

                case NotificationType.Error:
                    MainBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E53935"));
                    IconText.Text = "✕";
                    IconText.Foreground = MainBorder.BorderBrush;
                    break;

                case NotificationType.Warning:
                    MainBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9800"));
                    IconText.Text = "!";
                    IconText.Foreground = MainBorder.BorderBrush;
                    break;

                case NotificationType.Info:
                    MainBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2196F3"));
                    IconText.Text = "ℹ";
                    IconText.Foreground = MainBorder.BorderBrush;
                    break;
            }

            Loaded += (s, e) =>
            {
                var anim = (Storyboard)Resources["FadeIn"];
                anim.Begin(this); // ✅ يبدأ على هذا التوست فقط
                StartTimer();
            };

            Unloaded += (s, e) =>
            {
                // ✅ حماية من تسريب التايمر
                if (_timer != null)
                {
                    _timer.Stop();
                    _timer.Tick -= Timer_Tick;
                    _timer = null;
                }
            };
        }

        private void StartTimer()
        {
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            _timer.Stop();
            CloseNotification();
        }

        private void CloseNotification()
        {
            var anim = (Storyboard)Resources["FadeOut"];
            anim.Begin(this); // ✅ يبدأ على هذا التوست فقط
        }

        private void FadeOut_Completed(object sender, EventArgs e)
        {
            if (Parent is Panel parent)
                parent.Children.Remove(this);
        }

        private void UserControl_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _timer?.Stop();
            CloseNotification();
        }
    }
}
