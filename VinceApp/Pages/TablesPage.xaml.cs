using Serilog;
using System;
using System.Linq;
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

        public TablesPage()
        {
            InitializeComponent();
            LoadTables();
        }

        private void LoadTables()
        {
            try
            {
                using (var context = new VinceSweetsDbContext())
                {
                    dgTables.ItemsSource = context.RestaurantTables.OrderBy(t => t.TableNumber).ToList();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "error with Table page loading");
                ToastControl.Show("معلومات", $"خطأ في تحميل البيانات", ToastControl.NotificationType.Error);
            }
        }

        // هذا الزر يعمل للإضافة وللحفظ بعد التعديل
        private void SaveTable_Click(object sender, RoutedEventArgs e)
        {
            string numberStr = txtTableNumber.Text.Trim();
            string name = txtTableName.Text.Trim();

            // التحقق من الإدخال
            if (string.IsNullOrEmpty(numberStr) || !int.TryParse(numberStr, out int tableNumber))
            {
                ToastControl.Show("معلومات", "يرجى إدخال رقم طاولة صحيح (أرقام فقط).", ToastControl.NotificationType.Error);
                return;
            }

            try
            {
                using (var context = new VinceSweetsDbContext())
                {
                    // التحقق من عدم تكرار رقم الطاولة (مع استثناء الطاولة الحالية في حالة التعديل)
                    bool exists = context.RestaurantTables.Any(t => t.TableNumber == tableNumber && t.Id != _editingId);
                    if (exists)
                    {
                        ToastControl.Show("معلومات", $"رقم الطاولة {tableNumber} مستخدم بالفعل!", ToastControl.NotificationType.Error);
                        
                        return;
                    }

                    if (_editingId == 0)
                    {
                        // === حالة إضافة جديدة ===
                        var newTable = new RestaurantTable
                        {
                            TableNumber = tableNumber,
                            TableName = name,
                            Status = 0
                        };
                        context.RestaurantTables.Add(newTable);
                        
                        ToastControl.Show("معلومات", "تمت الإضافة بنجاح", ToastControl.NotificationType.Success);
                    }
                    else
                    {
                        // === حالة حفظ التعديل ===
                        var tableToEdit = context.RestaurantTables.Find(_editingId);
                        if (tableToEdit != null)
                        {
                            tableToEdit.TableNumber = tableNumber;
                            tableToEdit.TableName = name;
                            // لا نعدل الـ Status هنا
                        }
                        ToastControl.Show("معلومات", "تم تعديل البيانات بنجاح", ToastControl.NotificationType.Success);
                        
                    }

                    context.SaveChanges();
                    ResetForm(); // تنظيف الحقول والعودة للوضع الافتراضي
                    LoadTables(); // تحديث الجدول
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "error with tables page saving");
                ToastControl.Show("معلومات", "فشل حفظ الطاولة", ToastControl.NotificationType.Error);
                
            }
        }

        // حدث عند الضغط على زر القلم (تعديل)
        private void EditTable_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                using (var context = new VinceSweetsDbContext())
                {
                    var table = context.RestaurantTables.Find(id);
                    if (table != null)
                    {
                        // تعبئة البيانات في الحقول
                        txtTableNumber.Text = table.TableNumber.ToString();
                        txtTableName.Text = table.TableName;

                        // تفعيل وضع التعديل
                        _editingId = id;

                        // تغيير شكل الزر ليدل على التعديل
                        btnSave.Content = "💾 حفظ التعديل";
                        btnSave.Background = new SolidColorBrush(Color.FromRgb(255, 152, 0)); // لون برتقالي
                        btnCancel.Visibility = Visibility.Visible; // إظهار زر الإلغاء
                    }
                }
            }
        }

        // زر إلغاء التعديل
        private void CancelEdit_Click(object sender, RoutedEventArgs e)
        {
            ResetForm();
        }

        // دالة لإعادة الصفحة للوضع الافتراضي
        private void ResetForm()
        {
            txtTableNumber.Clear();
            txtTableName.Clear();
            _editingId = 0;

            btnSave.Content = "➕ إضافة";
            btnSave.Background = new SolidColorBrush(Color.FromRgb(76, 175, 80)); // العودة للأخضر
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
                                var table = context.RestaurantTables.Find(id);
                                if (table != null)
                                {
                                    if (table.Status != 0)
                                    {
                                        ToastControl.Show("معلومات", "لا يمكن حذف الطاولة وهي مشغولة حالياً!", ToastControl.NotificationType.Warning);
                                        return;
                                    }

                                    context.RestaurantTables.Remove(table);
                                    context.SaveChanges();

                                    // إذا كنا نعدل نفس الطاولة التي قمنا بحذفها، يجب تصفية الحقول
                                    if (_editingId == id) ResetForm();

                                    LoadTables();
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