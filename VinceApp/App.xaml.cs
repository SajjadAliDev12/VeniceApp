using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using VinceApp.Data;

namespace VinceApp
{
    public partial class App : Application
    {
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
        private void InitializeDatabase()
        {
            try
            {
                using var context = new VinceSweetsDbContext();
                context.Database.Migrate(); // النسخة المتزامنة

                // يمكنك استدعاء CleanupTablesAsync بشكل منفصل إذا أردت
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to migrate DB on startup");
                MessageBox.Show("فشل في الاتصال بقاعدة البيانات.");
                Shutdown();
            }
        }
        /// <summary>
        /// دالة غير متزامنة لتهيئة الداتا بيس وتنظيف الطاولات دون التأثير على الواجهة
        /// </summary>
        private async Task InitializeDatabaseInBackgroundAsync()
        {
            try
            {
                await Task.Run(async () =>
                {
                    using var context = new VinceSweetsDbContext();

                    // تطبيق التحديثات على قاعدة البيانات (Migration)
                    await context.Database.MigrateAsync();

                    // تنظيف الطاولات والطلبات المعلقة
                    await CleanupTablesAsync(context);
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "فشل في تهيئة قاعدة البيانات في الخلفية (Migrate/Cleanup)");

                // تنبيه المستخدم (يجب استخدام Dispatcher لأننا في خيط خلفي)
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

        /// <summary>
        /// تنظيف الطلبات القديمة وتصحيح حالات الطاولات العالقة
        /// </summary>
        private async Task CleanupTablesAsync(VinceSweetsDbContext context)
        {
            try
            {
                // 1. إغلاق الطلبات القديمة (من أيام سابقة) ولم تدفع
                var oldOrders = await context.Orders
                    .Where(o => !o.isPaid && o.OrderDate.Value.Date < DateTime.Now.Date)
                    .ToListAsync();

                if (oldOrders.Any())
                {
                    foreach (var order in oldOrders)
                    {
                        order.isServed = true; // نعتبرها مُسلمة لإغلاقها منطقياً

                        if (order.TableId.HasValue)
                        {
                            var tableToFree = await context.RestaurantTables.FindAsync(order.TableId.Value);
                            if (tableToFree != null)
                                tableToFree.Status = 0; // تحرير الطاولة
                        }
                    }
                }

                // 2. تصحيح حالات الطاولات "الوهمية" (التي تظهر مشغولة ولا يوجد طلب عليها اليوم)
                var tables = await context.RestaurantTables.ToListAsync();

                foreach (var table in tables)
                {
                    if (table.Status == 1) // إذا كانت مشغولة
                    {
                        // نتأكد هل يوجد طلب حقيقي فعال لها اليوم؟
                        bool hasActiveOrder = await context.Orders.AnyAsync(o =>
                            o.TableId == table.Id &&
                            !o.isPaid &&
                            o.OrderDate.Value.Date == DateTime.Now.Date);

                        // إذا لم يوجد طلب، نعيدها فارغة
                        if (!hasActiveOrder)
                        {
                            table.Status = 0;
                        }
                    }
                }

                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // نسجل الخطأ ونعيد رميه ليتم التقاطه في الدالة الرئيسية
                Log.Error(ex, "خطأ أثناء تنظيف الطاولات (CleanupTablesAsync)");
                throw;
            }
        }

        // ================= معالجات الأخطاء =================

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Log.Error(e.Exception, "Uncaught UI Exception");
            e.Handled = true; // منع إغلاق البرنامج
            MessageBox.Show("حدث خطأ غير متوقع، تم تسجيله في السجلات.", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            Log.Error(ex, "Fatal Non-UI Exception");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log.CloseAndFlush();
            base.OnExit(e);
        }
    }
}