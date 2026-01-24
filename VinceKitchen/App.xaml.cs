using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;

namespace VinceKitchen
{
    public partial class App : Application
    {
        private static Mutex? _singleInstanceMutex;

        // ✅ اسم فريد للـ Kitchen (مختلف عن POS الرئيسي)
        private const string MutexNameGlobal = @"Global\VinceKitchen_SingleInstance_658261A6-5A95-4CEA-AD66-3A8C232F7B9B";
        private const string MutexNameLocal = @"Local\VinceKitchen_SingleInstance_658261A6-5A95-4CEA-AD66-3A8C232F7B9B";

        protected override void OnStartup(StartupEventArgs e)
        {
            // ✅ امنع تشغيل نسخة ثانية قبل أي نافذة
            if (!EnsureSingleInstance())
            {
                Shutdown();
                return;
            }

            base.OnStartup(e);
        }

        private bool EnsureSingleInstance()
        {
            try
            {
                bool createdNew;
                _singleInstanceMutex = new Mutex(true, MutexNameGlobal, out createdNew);

                if (!createdNew)
                {
                    ActivateExistingInstance();
                    return false;
                }

                return true;
            }
            catch (UnauthorizedAccessException)
            {
                // ✅ fallback إلى Local إذا Global سبب مشكلة صلاحيات
                bool createdNew;
                _singleInstanceMutex = new Mutex(true, MutexNameLocal, out createdNew);

                if (!createdNew)
                {
                    ActivateExistingInstance();
                    return false;
                }

                return true;
            }
            catch
            {
                // إذا حدث خطأ غير متوقع، لا تمنع التشغيل
                return true;
            }
        }

        private void ActivateExistingInstance()
        {
            try
            {
                var current = Process.GetCurrentProcess();

                var existing = Process.GetProcessesByName(current.ProcessName)
                    .FirstOrDefault(p => p.Id != current.Id);

                if (existing == null) return;

                IntPtr hWnd = existing.MainWindowHandle;
                if (hWnd == IntPtr.Zero) return;

                // إذا كانت minimized رجعها
                ShowWindow(hWnd, SW_RESTORE);

                // اجعلها في المقدمة
                SetForegroundWindow(hWnd);
            }
            catch
            {
                // تجاهل
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                _singleInstanceMutex?.ReleaseMutex();
                _singleInstanceMutex?.Dispose();
            }
            catch { /* ignore */ }

            base.OnExit(e);
        }

        // ================= WinAPI =================
        private const int SW_RESTORE = 9;

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    }
}
