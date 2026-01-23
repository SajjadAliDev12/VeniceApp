using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore; // مهم جداً
using Serilog;
using VinceApp.Data;
using VinceApp.Data.Models;

namespace VinceApp.Pages
{
    public partial class CategoriesPage : Page
    {
        // إذا كان 0، يعني نحن نضيف جديداً
        // إذا كان أكبر من 0، يعني نحن نعدل تصنيفاً موجوداً
        private int _selectedId = 0;

        public CategoriesPage()
        {
            InitializeComponent();
            LoadCategories();
        }

        private void LoadCategories()
        {
            try
            {
                using (var context = new VinceSweetsDbContext())
                {
                    // جلب البيانات وعرضها في الجدول
                    var list = context.Categories.ToList();
                    dgCategories.ItemsSource = list;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "database connection error with categories page");
                ToastControl.Show("خطأ", "خطأ في التحميل", ToastControl.NotificationType.Error);
                
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            string name = txtName.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                ToastControl.Show("الاسم مطلوب", "يرجى كتابة الاسم", ToastControl.NotificationType.Info);
                //MessageBox.Show("يرجى كتابة اسم التصنيف", "OK", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                using (var context = new VinceSweetsDbContext())
                {
                    if (_selectedId == 0)
                    {
                        // === إضافة جديد ===
                        var newCat = new Category { Name = name };
                        context.Categories.Add(newCat);
                    }
                    else
                    {
                        // === تعديل موجود ===
                        var cat = context.Categories.Find(_selectedId);
                        if (cat != null)
                        {
                            cat.Name = name;
                        }
                    }

                    context.SaveChanges();

                    // تنظيف الحقول وإعادة التحميل
                    ClearFields();
                    LoadCategories();
                    ToastControl.Show("نجاح", "تم الحفظ بنجاح ", ToastControl.NotificationType.Success);
                    //MessageBox.Show("تم الحفظ بنجاح","OK",MessageBoxButton.OK,MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "error with categories page");
                ToastControl.Show("خطأ", "خطأ في التحميل", ToastControl.NotificationType.Error);
            }
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            // عند الضغط على زر تعديل في الجدول
            if (sender is Button btn && btn.Tag is int id)
            {
                using (var context = new VinceSweetsDbContext())
                {
                    var cat = context.Categories.Find(id);
                    if (cat != null)
                    {
                        txtName.Text = cat.Name;
                        _selectedId = cat.Id; // وضعنا الـ ID لنعرف أننا في وضع التعديل
                    }
                }
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                if (MessageBox.Show("هل أنت متأكد من الحذف؟", "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (var context = new VinceSweetsDbContext())
                        {
                            // 1. حماية: منع الحذف إذا كان التصنيف يحتوي منتجات
                            // لأن حذف التصنيف سيجعل المنتجات يتيمة (أو يسبب خطأ FK)
                            bool hasProducts = context.Products.Any(p => p.CategoryId == id);
                            if (hasProducts)
                            {
                                ToastControl.Show( "منع الحذف","لا يمكن حذف هذا التصنيف لأنه يحتوي على منتجات.\nيرجى حذف المنتجات أو نقلها أولاً.", ToastControl.NotificationType.Warning);
                                
                                return;
                            }

                            // 2. الحذف
                            var cat = context.Categories.Find(id);
                            if (cat != null)
                            {
                                context.Categories.Remove(cat);
                                context.SaveChanges();
                                LoadCategories();
                                ClearFields(); // في حال كنا نعدله ثم حذفناه
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "error with categories page");
                        ToastControl.Show("خطأ", "لا يمكن الحذف", ToastControl.NotificationType.Error);
                    }
                }
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            ClearFields();
        }

        private void ClearFields()
        {
            txtName.Text = "";
            _selectedId = 0; // العودة لوضع الإضافة
        }
    }
}