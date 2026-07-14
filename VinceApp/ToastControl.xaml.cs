using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Threading.Tasks;
using VinceApp.Services;

namespace VinceApp
{
    public partial class ToastControl : UserControl
    {
        private DispatcherTimer _timer;
        private static DateTime _lastSoundPlayedTime = DateTime.MinValue;
        private const int ToastDurationSeconds = 10;
        private Storyboard _lifeBarStoryboard;

        // مرجع للنافذة المستقلة التي ستحمل هذا الـ UserControl
        private Window _parentToastWindow;

        public enum NotificationType
        {
            Success,
            Error,
            Warning,
            Info
        }

        // الدالة المعدلة برمجياً لفتح نافذة تطفو فوق الجميع Topmost
        public static void Show(string title, string message, NotificationType type)
        {
            // 1. تشغيل أصوات التنبيهات (نفس كودك القديم السليم)
            string soundFile = "";
            if (Application.Current.Properties["DisableSounds"] as bool? != true)
            {
                switch (type)
                {
                    case NotificationType.Success: soundFile = FIlePathFinder.GetPath("Windows Notify.wav"); break;
                    case NotificationType.Error: soundFile = FIlePathFinder.GetPath("Windows Pop-up Blocked.wav"); break;
                    case NotificationType.Warning: soundFile = FIlePathFinder.GetPath("Windows Unlock.wav"); break;
                    default: soundFile = FIlePathFinder.GetPath("Windows Notify.wav"); break;
                }
            }

            if ((DateTime.Now - _lastSoundPlayedTime).TotalMilliseconds > 1000)
            {
                _lastSoundPlayedTime = DateTime.Now;
                Task.Run(() =>
                {
                    try { using (var player = new System.Media.SoundPlayer(soundFile)) player.PlaySync(); }
                    catch { /* تجاهل أخطاء تشغيل الصوت */ }
                });
            }

            // 2. المحرك المركزي الجديد: بناء نافذة عائمة شفافة تماماً وإطلاقها بالـ Dispatcher
            Application.Current.Dispatcher.Invoke(() =>
            {
                var toastControl = new ToastControl(title, message, type);

                // إنشاء نافذة شفافة في الخلفية لا تحتوي على إطار وتطفو فوق كل شيء
                var popupWindow = new Window
                {
                    WindowStyle = WindowStyle.None,
                    AllowsTransparency = true,
                    Background = Brushes.Transparent,
                    ShowInTaskbar = false,
                    Topmost = true, // 🌟 القفل المركزي: يجعل الإشعار يطفو فوق النوافذ المفتوحة بـ ShowDialog
                    SizeToContent = SizeToContent.WidthAndHeight,
                    ResizeMode = ResizeMode.NoResize,
                    Content = toastControl,
                    FlowDirection = FlowDirection.RightToLeft
                };

                // ربط المرجع المتبادل للإغلاق النظيف
                toastControl._parentToastWindow = popupWindow;

                // احتساب موقع ظهور الإشعار في زاوية الشاشة اليمنى السفلية (أعلى شريط المهام بـ 40 بكسل)
                var workingArea = SystemParameters.WorkArea;
                popupWindow.Left = workingArea.Right - toastControl.Width - 20; // 20 بكسل هامش من اليمين

                // حساب تراكمي ذكي: لكي لا تظهر الإشعارات فوق بعضها إذا انطلقت معاً
                var openToastsCount = Application.Current.Windows.OfType<Window>().Count(w => w.Content is ToastControl && w.IsVisible);
                popupWindow.Top = workingArea.Top + 20 + (toastControl.Height + 10) * openToastsCount;

                // إطلاق النافذة بـ Show (وليس ShowDialog) لكي لا تعطل عمل الكاشير
                popupWindow.Show();
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
                    IconText.Text = "✓"; IconText.Foreground = MainBorder.BorderBrush; break;
                case NotificationType.Error:
                    MainBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E53935"));
                    IconText.Text = "✕"; IconText.Foreground = MainBorder.BorderBrush; break;
                case NotificationType.Warning:
                    MainBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9800"));
                    IconText.Text = "!"; IconText.Foreground = MainBorder.BorderBrush; break;
                case NotificationType.Info:
                    MainBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2196F3"));
                    IconText.Text = "ℹ"; IconText.Foreground = MainBorder.BorderBrush; break;
            }

            Loaded += (s, e) =>
            {
                ((Storyboard)Resources["FadeIn"]).Begin(this);
                StartLifeBar();
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
            CleanupTimer();
            StopLifeBar();

            var anim = (Storyboard)Resources["FadeOut"];
            anim.Begin(this);
        }

        private void FadeOut_Completed(object sender, EventArgs e)
        {
            // إغلاق النافذة الحاضنة بالكامل وتحرير موارد الذاكرة
            if (_parentToastWindow != null)
            {
                _parentToastWindow.Close();
                _parentToastWindow = null;
            }
        }

        private void UserControl_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            CloseNotification();
        }

        private void StartLifeBar()
        {
            StopLifeBar();
            _lifeBarStoryboard = (Storyboard)Resources["LifeBar"];
            foreach (var tl in _lifeBarStoryboard.Children.OfType<DoubleAnimation>())
            {
                tl.Duration = TimeSpan.FromSeconds(ToastDurationSeconds);
            }
            _lifeBarStoryboard.Begin(this, true);
        }

        private void StopLifeBar()
        {
            if (_lifeBarStoryboard != null)
            {
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