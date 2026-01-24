using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.IO;
using VinceApp.Services;

namespace VinceApp
{
    public partial class ToastControl : UserControl
    {
        private DispatcherTimer _timer;

        // ✅ مدة التوست (ثواني) — خليها ثابتة هنا وتتحكم بها من مكان واحد
        private const int ToastDurationSeconds = 10;

        // ✅ مرجع لستوري بورد الشريط حتى نوقفه/نعيد تشغيله
        private Storyboard _lifeBarStoryboard;

        public enum NotificationType
        {
            Success,
            Error,
            Warning,
            Info
        }

        public static void Show(string title, string message, NotificationType type)
        {
            string soundFile = "";
            if (Application.Current.Properties["DisableSounds"] as bool? != true)
            {
                switch (type)
                {
                    case NotificationType.Success:
                        soundFile = FIlePathFinder.GetPath("Windows Notify.wav");
                        break;
                    case NotificationType.Error:
                        soundFile = FIlePathFinder.GetPath("Windows Pop-up Blocked.wav");
                        break;
                    case NotificationType.Warning:
                        soundFile = FIlePathFinder.GetPath("Windows Unlock.wav");
                        break;
                    default:
                        soundFile = FIlePathFinder.GetPath("Windows Notify.wav");
                        break;
                }
            }

            if (File.Exists(soundFile))
            {
                using (var player = new System.Media.SoundPlayer(soundFile))
                {
                    player.Play();
                }
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                Window targetWindow =
                    Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
                    ?? Application.Current.MainWindow;

                if (targetWindow == null) return;

                var container = targetWindow.FindName("NotificationArea") as Panel;

                if (container == null)
                {
                    targetWindow = Application.Current.MainWindow;
                    container = targetWindow?.FindName("NotificationArea") as Panel;
                }

                if (container == null) return;

                var toast = new ToastControl(title, message, type);

                container.Children.Insert(0, toast);
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
                // ✅ Fade In
                ((Storyboard)Resources["FadeIn"]).Begin(this);

                // ✅ شغّل شريط المدة بنفس مدة التوست
                StartLifeBar();

                // ✅ شغّل التايمر
                StartTimer();
            };

            Unloaded += (s, e) =>
            {
                CleanupTimer();
                StopLifeBar();
            };
        }

        private void StartTimer()
        {
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(ToastDurationSeconds) };
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
            // ✅ عند الإغلاق، أوقف التايمر والشريط
            CleanupTimer();
            StopLifeBar();

            var anim = (Storyboard)Resources["FadeOut"];
            anim.Begin(this);
        }

        private void FadeOut_Completed(object sender, EventArgs e)
        {
            if (Parent is Panel parent)
                parent.Children.Remove(this);
        }

        private void UserControl_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            CloseNotification();
        }

        // ===========================
        // ✅ LifeBar التحكم بالشريط
        // ===========================

        private void StartLifeBar()
        {
            // ✅ لو موجود قديم، وقفه
            StopLifeBar();

            _lifeBarStoryboard = (Storyboard)Resources["LifeBar"];

            // ✅ اضبط Duration ديناميكياً (لو غيرت مدة التوست لاحقاً ما تحتاج تعدل XAML)
            foreach (var tl in _lifeBarStoryboard.Children.OfType<DoubleAnimation>())
            {
                tl.Duration = TimeSpan.FromSeconds(ToastDurationSeconds);
            }

            // ✅ ابدأ (يبدأ من 1 إلى 0)
            _lifeBarStoryboard.Begin(this, true);
        }

        private void StopLifeBar()
        {
            if (_lifeBarStoryboard != null)
            {
                // Stop(this) يوقفه فوراً
                _lifeBarStoryboard.Stop(this);
                _lifeBarStoryboard = null;
            }
        }

        private void CleanupTimer()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Tick -= Timer_Tick;
                _timer = null;
            }
        }
    }
}
