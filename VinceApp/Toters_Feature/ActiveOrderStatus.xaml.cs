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
                                order.isReady = true;
                            }
                            await context.SaveChangesAsync();
                        }
                    }
                }
            }catch(Exception ex) { Log.Error("error with completing toters order"); }
            finally { this.Close(); }
        }
        private async void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_orderID > 0)
                {
                    using (var context = new VinceSweetsDbContext())
                    {
                        var order = await context.Orders.FirstOrDefaultAsync(o => o.Id == _orderID);
                        if (order != null && order.isPaid && !order.isDeleted && !order.isDone)
                        {
                            order.isPaid = false;
                        }
                        await context.SaveChangesAsync();
                    }
                }
                
                    MainWindow mainWindow = new MainWindow(_orderID, null, null, null, true);
                    this.Close();
                    mainWindow.ShowDialog();
            }
            catch (Exception ex) { Log.Error("error in editing toters order"); }
            finally { this.Close(); }
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
                                order.isPaid = true; order.isServed = true;
                                order.isReady = true;
                            }
                            await context.SaveChangesAsync();
                        }
                    }
                }
            }
            catch (Exception ex) { Log.Error("error with deleting toters order"); }
            finally { this.Close(); }
        }
    }
}
