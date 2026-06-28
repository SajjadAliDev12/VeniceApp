using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using Serilog;
using VinceApp.Data;
using VinceApp.Data.Models;
using static VinceApp.Data.Enums.Enums; // تأكد من مسار الـ Enum في مشروعك

namespace VinceApp.Pages
{
    public partial class CategoriesPage : Page
    {
        private int _selectedId = 0;

        public CategoriesPage()
        {
            InitializeComponent();
        }

        private async Task LoadCategories()
        {
            try
            {
                using (var context = new VinceSweetsDbContext())
                {
                    var list = await context.Categories.AsNoTracking().ToListAsync();
                    dgCategories.ItemsSource = list;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading categories");
                ToastControl.Show("خطأ", "فشل تحميل البيانات", ToastControl.NotificationType.Error);
            }
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            string name = txtName.Text.Trim();

            // منطق جلب القيمة من الكومبوبوكس
            EnPrinter selectedPrinter = EnPrinter.EnNone;
            if (cmbPrinters.SelectedValue != null)
            {
                selectedPrinter = (EnPrinter)int.Parse(cmbPrinters.SelectedValue.ToString());
            }

            if (string.IsNullOrEmpty(name))
            {
                ToastControl.Show("تنبيه", "يرجى إدخال اسم التصنيف", ToastControl.NotificationType.Info);
                return;
            }

            try
            {
                using (var context = new VinceSweetsDbContext())
                {
                    bool exists = await context.Categories.AnyAsync(c => c.Name == name && c.Id != _selectedId);
                    if (exists)
                    {
                        ToastControl.Show("تنبيه", "هذا الاسم موجود مسبقاً", ToastControl.NotificationType.Warning);
                        return;
                    }

                    if (_selectedId == 0)
                    {
                        var newCat = new Category
                        {
                            Name = name,
                            Printer = selectedPrinter
                        };
                        context.Categories.Add(newCat);
                    }
                    else
                    {
                        var cat = await context.Categories.FindAsync(_selectedId);
                        if (cat != null)
                        {
                            cat.Name = name;
                            cat.Printer = selectedPrinter;
                        }
                    }

                    await context.SaveChangesAsync();

                    ClearFields();
                    await LoadCategories();
                    ToastControl.Show("نجاح", "تم حفظ التصنيف بنجاح", ToastControl.NotificationType.Success);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error saving category");
                ToastControl.Show("خطأ", "فشل في عملية الحفظ", ToastControl.NotificationType.Error);
            }
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                using (var context = new VinceSweetsDbContext())
                {
                    var cat = context.Categories.Find(id);
                    if (cat != null)
                    {
                        _selectedId = cat.Id;
                        txtName.Text = cat.Name;

                        // اختيار القيمة في الكومبوبوكس بناءً على القيمة المخزنة
                        cmbPrinters.SelectedValue = ((int)cat.Printer).ToString();
                    }
                }
            }
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                var parentWindow = Window.GetWindow(this) as AdminWindow;
                if (parentWindow != null)
                {
                    if (await parentWindow.ShowConfirmMessage("تأكيد", "هل تريد حذف هذا التصنيف؟"))
                    {
                        try
                        {
                            using (var context = new VinceSweetsDbContext())
                            {
                                if (await context.Products.AnyAsync(p => p.CategoryId == id))
                                {
                                    ToastControl.Show("منع الحذف", "لا يمكن حذف تصنيف يحتوي على منتجات", ToastControl.NotificationType.Warning);
                                    return;
                                }

                                var cat = await context.Categories.FindAsync(id);
                                if (cat != null)
                                {
                                    context.Categories.Remove(cat);
                                    await context.SaveChangesAsync();
                                    await LoadCategories();
                                    ClearFields();
                                    ToastControl.Show("نجاح", "تم الحذف بنجاح", ToastControl.NotificationType.Success);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Error deleting category");
                            ToastControl.Show("خطأ", "فشل الحذف", ToastControl.NotificationType.Error);
                        }
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
            txtName.Text = string.Empty;
            cmbPrinters.SelectedIndex = 0; // يعود لـ EnNone افتراضياً
            _selectedId = 0;
        }

        private async void Page_Initialized(object sender, EventArgs e)
        {
            await LoadCategories();
        }
    }
}