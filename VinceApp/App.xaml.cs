using Microsoft.EntityFrameworkCore;
using System.Configuration;
using System.Data;
using System.Windows;
using VinceApp.Data;

namespace VinceApp
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            using (var context = new VinceSweetsDbContext())
            {
                //  يطبق أي تغييرات جديدة في الداتا بيس تلقائياً
                try
                { context.Database.Migrate();
                }catch (Exception ex) 
                {
                    MessageBox.Show("خطأ في قاعدة البيانات لا يمكن تشغيل البرنامج!","خطأ",MessageBoxButton.OK, MessageBoxImage.Error);
                }
                }
            try
            {
                // عملية التنظيف عند بدء التشغيل
                CleanupTables();
            }
            catch (System.Exception)
            {
                MessageBox.Show(
                    "تعذر الاتصال بقاعدة البيانات لا يمكن تشغيل البرنامج",
                    "خطأ في بدء التشغيل",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );

                // لا نكمل تشغيل البرنامج بدون قاعدة بيانات
                Shutdown();
                return;
            }

            TablesWindow tablesWindow = new TablesWindow();
            tablesWindow.Show();
        }

        private void CleanupTables()
        {
            try
            {
                using (var context = new VinceSweetsDbContext())
                {
                    //context.Database.EnsureCreated();

                    var oldOrders = context.Orders
                        .Where(o => o.OrderStatus == "Open" && o.OrderDate.Value.Date < System.DateTime.Now.Date)
                        .ToList();

                    if (oldOrders.Any())
                    {
                        foreach (var order in oldOrders)
                        {
                            order.OrderStatus = "Cancelled";
                            if (order.TableId.HasValue)
                            {
                                var tableToFree = context.RestaurantTables.Find(order.TableId.Value);
                                if (tableToFree != null)
                                    tableToFree.Status = 0;
                            }
                        }
                    }

                    var tables = context.RestaurantTables.ToList();

                    foreach (var table in tables)
                    {
                        if (table.Status == 1)
                        {
                            bool hasActiveOrder = context.Orders.Any(o =>
                                o.TableId == table.Id &&
                                o.OrderStatus == "Open" &&
                                o.OrderDate.Value.Date == System.DateTime.Now.Date);

                            if (!hasActiveOrder)
                            {
                                table.Status = 0;
                            }
                        }
                    }

                    context.SaveChanges();
                }
            }
            catch (System.Exception)
            {
                // نعيد رمي الخطأ ليتم التعامل معه في OnStartup
                throw;
            }
        }
    }
}
