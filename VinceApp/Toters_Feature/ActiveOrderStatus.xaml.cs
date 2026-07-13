using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using VinceApp.Data.Models;
using VinceApp;
using Serilog;
namespace VinceApp.Toters_Feature
{
    /// <summary>
    /// Interaction logic for ActiveOrderStatus.xaml
    /// </summary>
    public partial class ActiveOrderStatus : Window
    {
        private int _orderID;
        
        public ActiveOrderStatus(int OrderID)
        {
            InitializeComponent();
            this._orderID = OrderID;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        
        private async void btnDone_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                
                if (await MyConfirmDialog.ShowAsync("تأكيد", "هل تود نقل الطلب الى الطلبات المكتملة؟"))
                {
                    if (_orderID > 0)
                    {
                        using (var context = new VinceSweetsDbContext())
                        {
                            var order = await context.Orders.FirstOrDefaultAsync(o => o.Id == _orderID);
                            if (order != null)
                            {
                                order.isDone = true;
                                order.isPaid = true;
                                order.isServed = true;
                            }
                            await context.SaveChangesAsync();
                        }
                    }
                }
            }catch(Exception ex) { Log.Error("error with completing toters order"); }
            finally { this.Close(); }
        }
        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (_orderID > 0)
            {
                MainWindow mainWindow = new MainWindow(_orderID, null, null, null, true);
                this.Close();
                mainWindow.ShowDialog();
            }
        }

        private async void BtnCancelOrder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (await MyConfirmDialog.ShowAsync("تأكيد", "هل تود نقل الطلب الى الطلبات الملغية؟"))
                {
                    if (_orderID > 0)
                    {
                        using (var context = new VinceSweetsDbContext())
                        {
                            var order = await context.Orders.FirstOrDefaultAsync(o => o.Id == _orderID);
                            if (order != null)
                            {
                                order.isDeleted = true;
                            }
                            await context.SaveChangesAsync();
                        }
                    }
                }
            }
            catch (Exception ex) { Log.Error("error with completing toters order"); }
            finally { this.Close(); }
        }
    }
}
