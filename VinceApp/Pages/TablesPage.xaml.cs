using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VinceApp.Data;
using VinceApp.Data.Models;

namespace VinceApp.Pages
{
    public partial class TablesPage : Page
    {
        // متغير لتخزين معرف الطاولة التي يتم تعديلها (0 = وضع إضافة جديد)
        private int _editingId = 0;
        private bool _isLoaded = false;

        public TablesPage()
        {
            InitializeComponent();

            // استدعاء التحميل في حدث Loaded أفضل من الـ Constructor
            this.Loaded += async (s, e) =>
            {
                if (_isLoaded) return;
                _isLoaded = true;
                await LoadTablesAsync();
            };
        }

        private async Task LoadTablesAsync()
        {
            try
            {
                using (var context = new VinceSweetsDbContext())
                {
                    // AsNoTracking لأننا نعرض البيانات فقط
                    dgTables.ItemsSource = await context.RestaurantTables
                        .AsNoTracking()
                        .OrderBy(t => t.TableNumber)
                        .ToListAsync();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "error with Table page loading");
                ToastControl.Show("معلومات", $"خطأ في تحميل البيانات", ToastControl.NotificationType.Error);
            }
        }

        private async void SaveTable_Click(object sender, RoutedEventArgs e)
        {
            string numberStr = txtTableNumber.Text.Trim();
            string name = txtTableName.Text.Trim();

            if (string.IsNullOrEmpty(numberStr) || !int.TryParse(numberStr, out int tableNumber))
            {
                ToastControl.Show("معلومات", "يرجى إدخال رقم طاولة صحيح (أرقام فقط).", ToastControl.NotificationType.Error);
                return;
            }

            try
            {
                using (var context = new VinceSweetsDbContext())
                {
                    // التحقق غير المتزامن
                    bool exists = await context.RestaurantTables.AnyAsync(t => t.TableNumber == tableNumber && t.Id != _editingId);
                    if (exists)
                    {
                        ToastControl.Show("معلومات", $"رقم الطاولة {tableNumber} مستخدم بالفعل!", ToastControl.NotificationType.Error);
                        return;
                    }

                    if (_editingId == 0)
                    {
                        // إضافة جديدة
                        var newTable = new RestaurantTable
                        {
                            TableNumber = tableNumber,
                            TableName = name,
                            Status = 0
                        };
                        await context.RestaurantTables.AddAsync(newTable);
                        ToastControl.Show("معلومات", "تمت الإضافة بنجاح", ToastControl.NotificationType.Success);
                    }
                    else
                    {
                        // تعديل (استخدام FindAsync)
                        var tableToEdit = await context.RestaurantTables.FindAsync(_editingId);
                        if (tableToEdit != null)
                        {
                            tableToEdit.TableNumber = tableNumber;
                            tableToEdit.TableName = name;
                        }
                        ToastControl.Show("معلومات", "تم تعديل البيانات بنجاح", ToastControl.NotificationType.Success);
                    }

                    await context.SaveChangesAsync();

                    ResetForm();
                    await LoadTablesAsync();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "error with tables page saving");
                ToastControl.Show("معلومات", "فشل حفظ الطاولة", ToastControl.NotificationType.Error);
            }
        }

        private async void EditTable_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                using (var context = new VinceSweetsDbContext())
                {
                    var table = await context.RestaurantTables.FindAsync(id);
                    if (table != null)
                    {
                        txtTableNumber.Text = table.TableNumber.ToString();
                        txtTableName.Text = table.TableName;
                        _editingId = id;

                        btnSave.Content = "💾 حفظ التعديل";
                        btnSave.Background = new SolidColorBrush(Color.FromRgb(255, 152, 0));
                        btnCancel.Visibility = Visibility.Visible;
                    }
                }
            }
        }

        private void CancelEdit_Click(object sender, RoutedEventArgs e)
        {
            ResetForm();
        }

        private void ResetForm()
        {
            txtTableNumber.Clear();
            txtTableName.Clear();
            _editingId = 0;

            btnSave.Content = "➕ إضافة";
            btnSave.Background = new SolidColorBrush(Color.FromRgb(76, 175, 80));
            btnCancel.Visibility = Visibility.Collapsed;
        }

        private async void DeleteTable_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                var parentWindow = Window.GetWindow(this) as AdminWindow;

                if (parentWindow != null)
                {
                    if (await parentWindow.ShowConfirmMessage("تأكيد الحذف", "هل أنت متأكد من حذف هذه الطاولة؟"))
                    {
                        try
                        {
                            using (var context = new VinceSweetsDbContext())
                            {
                                var table = await context.RestaurantTables.FindAsync(id);
                                if (table != null)
                                {
                                    if (table.Status != 0)
                                    {
                                        ToastControl.Show("معلومات", "لا يمكن حذف الطاولة وهي مشغولة حالياً!", ToastControl.NotificationType.Warning);
                                        return;
                                    }

                                    context.RestaurantTables.Remove(table);
                                    await context.SaveChangesAsync();

                                    if (_editingId == id) ResetForm();

                                    await LoadTablesAsync();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "error with tables page delete");
                            ToastControl.Show("معلومات", "لا يمكن حذف الطاولة ", ToastControl.NotificationType.Error);
                        }
                    }
                }
            }
        }
    }
}