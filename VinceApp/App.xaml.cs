using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using VinceApp.Data;

namespace VinceApp
{
    public partial class App : Application
    {
        // ✅ Single Instance
        private static Mutex? _singleInstanceMutex;

        // ✅ اسم فريد للتطبيق (غيّره إذا تريد)
        // Global: يمنع تعدد النسخ حتى لو تعددت Sessions على نفس الجهاز
        // Local : يمنع تعدد النسخ لنفس المستخدم فقط
        private const string MutexNameGlobal = @"Global\VenicePOS_SingleInstance_213C405F-29A4-469B-B7BA-0E3F07D6622A";
        private const string MutexNameLocal = @"Local\VenicePOS_SingleInstance_213C405F-29A4-469B-B7BA-0E3F07D6622A";

        public App()
        {
            // إعداد نظام اللوج (Serilog)
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Error()
                .WriteTo.File("Logs/log-.txt",
                              rollingInterval: RollingInterval.Day,
                              retainedFileCountLimit: 30)
                .CreateLogger();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            // ✅ 1) امنع فتح نسخة ثانية (قبل أي نافذة أو DB أو Wizard)
            if (!EnsureSingleInstance())
            {
                Shutdown();
                return;
            }

            base.OnStartup(e);

            // معالجة الأخطاء العامة (Global Exception Handlers)
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            // عدم الإغلاق التلقائي حتى نحدد النافذة الرئيسية
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

                // 3. تشغيل تهيئة قاعدة البيانات وتنظيفها في الخلفية (Async)
                _ = InitializeDatabaseInBackgroundAsync();
            }
            else
            {
                Shutdown();
            }
        }

        /// <summary>
        /// ✅ يمنع تشغيل أكثر من نسخة
        /// </summary>
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

        private async Task InitializeDatabaseInBackgroundAsync()
        {
            try
            {
                await Task.Run(async () =>
                {
                    using var context = new VinceSweetsDbContext();
                    await context.Database.MigrateAsync();
                    await CleanupTablesAsync(context);
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "فشل في تهيئة قاعدة البيانات في الخلفية (Migrate/Cleanup)");

                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(
                        "تعذر الاتصال بقاعدة البيانات أو تهيئتها.\nيرجى التأكد من إعدادات الاتصال.",
                        "تنبيه",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                });
            }
        }

        private async Task CleanupTablesAsync(VinceSweetsDbContext context)
        {
            try
            {
                var oldOrders = await context.Orders
                    .Where(o => !o.isPaid && o.OrderDate.Value.Date < DateTime.Now.Date)
                    .ToListAsync();

                if (oldOrders.Any())
                {
                    foreach (var order in oldOrders)
                    {
                        order.isServed = true;

                        if (order.TableId.HasValue)
                        {
                            var tableToFree = await context.RestaurantTables.FindAsync(order.TableId.Value);
                            if (tableToFree != null)
                                tableToFree.Status = 0;
                        }
                    }
                }

                var tables = await context.RestaurantTables.ToListAsync();

                foreach (var table in tables)
                {
                    if (table.Status == 1)
                    {
                        bool hasActiveOrder = await context.Orders.AnyAsync(o =>
                            o.TableId == table.Id &&
                            !o.isPaid &&
                            o.OrderDate.Value.Date == DateTime.Now.Date);

                        if (!hasActiveOrder)
                            table.Status = 0;
                    }
                }

                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "خطأ أثناء تنظيف الطاولات (CleanupTablesAsync)");
                throw;
            }
        }

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