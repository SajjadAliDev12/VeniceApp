using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using VinceApp.Data.Models;

namespace VinceApp
{
    public partial class App : Application
    {
        
        private static Mutex? _singleInstanceMutex;

        private const string MutexNameGlobal = @"Global\VenicePOS_SingleInstance_213C405F-29A4-469B-B7BA-0E3F07D6622A";
        private const string MutexNameLocal = @"Local\VenicePOS_SingleInstance_213C405F-29A4-469B-B7BA-0E3F07D6622A";

        public App()
        {
            
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Error()
                .WriteTo.File("Logs/log-.txt",
                              rollingInterval: RollingInterval.Day,
                              retainedFileCountLimit: 30)
                .CreateLogger();
        }

        protected override void OnStartup(StartupEventArgs e)
        {

            if (!EnsureSingleInstance())
            {
                Shutdown();
                return;
            }

            base.OnStartup(e);


            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            if (!VinceApp.Services.FirstRunGate.RunWizardIfNeeded())
            {
                Shutdown();
                return;
            }

            InitializeDatabase();

            // 1. فتح نافذة تسجيل الدخول
            var loginWindow = new LoginWindow();
            bool? result = loginWindow.ShowDialog();

            if (result == true)
            {
                // 2. إذا نجح الدخول، نفتح الواجهة الرئيسية للطاولات
                var tablesWindow = new TablesWindow();
                this.MainWindow = tablesWindow;
                tablesWindow.Show();

                // إعادة وضع الإغلاق ليكون مرتبطاً بالنافذة الرئيسية
                this.ShutdownMode = ShutdownMode.OnMainWindowClose;

            }
            else
            {
                Shutdown();
            }
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
                // أحياناً Global يحتاج صلاحيات على بعض الأجهزة/الإعدادات
                // ✅ نعمل fallback إلى Local
                bool createdNew;
                _singleInstanceMutex = new Mutex(true, MutexNameLocal, out createdNew);

                if (!createdNew)
                {
                    ActivateExistingInstance();
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to create Single Instance Mutex.");
                
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

                // اجعلها فوق
                SetForegroundWindow(hWnd);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to activate existing instance.");
            }
        }

        private void InitializeDatabase()
        {
            while (true)
            {
                try
                {
                    using var context = new VinceSweetsDbContext();
                    context.Database.Migrate();

                   
                    break; 
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Startup Database Connection Failed");
                    string msg = $"فشل الاتصال بقاعدة البيانات!\n\n" +
                                 $"السبب المحتمل: السيرفر متوقف أو بيانات الاتصال خاطئة.\n\n" +
                                 $"التفاصيل: {ex.Message}\n\n" +
                                 $"• اضغط (نعم) للمحاولة مرة أخرى.\n" +
                                 $"• اضغط (لا) لتعديل إعدادات السيرفر.\n" +
                                 $"• اضغط (إلغاء الأمر) للخروج.";

                    var result = MessageBox.Show(msg, "خطأ حرج في الاتصال",
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Error);

                    if (result == MessageBoxResult.Yes)
                    {
                        continue;
                    }
                    else if (result == MessageBoxResult.No)
                    {
                        // فتح نافذة الإعدادات
                        var configWindow = new ServerConfigWindow();
                        if (configWindow.ShowDialog() == true)
                        {
                            continue;
                        }
                        else
                        {
                            // المستخدم فتح الإعدادات وأغلقها بدون حفظ.. نعتبرها محاولة خروج
                            Shutdown();
                            return;
                        }
                    }
                    else // Cancel
                    {
                        Shutdown();
                        return;
                    }
                }
            }
        }

        //private async Task InitializeDatabaseInBackgroundAsync()
        //{
        //    try
        //    {
        //        await Task.Run(async () =>
        //        {
        //            using var context = new VinceSweetsDbContext();
        //            await context.Database.MigrateAsync();
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error(ex, "فشل في تهيئة قاعدة البيانات في الخلفية (Migrate/Cleanup)");

        //        Dispatcher.Invoke(() =>
        //        {
        //            MessageBox.Show(
        //                "تعذر الاتصال بقاعدة البيانات أو تهيئتها.\nيرجى التأكد من إعدادات الاتصال.",
        //                "تنبيه",
        //                MessageBoxButton.OK,
        //                MessageBoxImage.Warning
        //            );
        //        });
        //    }
        //}

        

        // ================= معالجات الأخطاء =================

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Log.Error(e.Exception, "Uncaught UI Exception");
            e.Handled = true;
            MessageBox.Show("حدث خطأ غير متوقع، تم تسجيله في السجلات.", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            Log.Error(ex, "Fatal Non-UI Exception");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // ✅ حرّر الميوتكس
            try
            {
                _singleInstanceMutex?.ReleaseMutex();
                _singleInstanceMutex?.Dispose();
            }
            catch { /* ignore */ }

            Log.CloseAndFlush();
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