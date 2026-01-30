using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using Serilog;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using VinceApp.Data;
using VinceApp.Data.Models;

namespace VinceApp.Pages
{
    public partial class ProductsPage : Page
    {
        private int _selectedId = 0;
        private string _selectedImageSourcePath = null; // مسار الصورة المختارة من جهاز المستخدم
        private string _currentDbImageName = null;      // اسم الصورة المحفوظة حالياً (للتعديل)

        public ProductsPage()
        {
            InitializeComponent();
            Loaded +=async(_,__) =>
            await LoadData();
        }
        
        // 1. تحميل البيانات (تصنيفات + منتجات)
        private async Task LoadData()
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                await using (var context = new VinceSweetsDbContext())
                {
                    // تحميل التصنيفات للـ ComboBox
                    var categories =await context.Categories.ToListAsync();
                    cmbCategories.ItemsSource = categories;

                    // تحميل المنتجات للجدول (مع عمل Include للتصنيف)
                    var products = await context.Products.Include(p => p.Category).ToListAsync();
                    dgProducts.ItemsSource = products;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "error with Products page Loading");
                ToastControl.Show("معلومات", "فشل تحميل البيانات", ToastControl.NotificationType.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        // 2. اختيار صورة من الجهاز
        private void BrowseImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp";

            if (dlg.ShowDialog() == true)
            {
                _selectedImageSourcePath = dlg.FileName;
                txtImageName.Text = Path.GetFileName(_selectedImageSourcePath);

                // عرض معاينة
                imgPreview.Source = new BitmapImage(new Uri(_selectedImageSourcePath));
            }
        }

        // 3. الحفظ (إضافة أو تعديل)
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            string name = txtName.Text.Trim();
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(txtPrice.Text) || cmbCategories.SelectedValue == null)
            {
                ToastControl.Show("معلومات", "يرجى ملء جميع الحقول المطلوبة (الاسم، السعر، التصنيف)", ToastControl.NotificationType.Info);
                
                return;
            }

            if (!decimal.TryParse(txtPrice.Text, out decimal price))
            {

                ToastControl.Show("معلومات", "صيغة السعر غير صحيحة", ToastControl.NotificationType.Info);
                return;
            }

            int catId = (int)cmbCategories.SelectedValue;
            bool isAvailable = chkIsAvailable.IsChecked ?? false; // القيمة الجديدة
            bool isKetchin = chkIsKetchin.IsChecked ?? false;
            try
            {
                using (var context = new VinceSweetsDbContext())
                {
                    bool exists = context.Products.Any(p => p.Name == name && p.Id != _selectedId);
                    if (exists)
                    {
                        ToastControl.Show("تنبيه", "اسم المنتج موجود مسبقاً", ToastControl.NotificationType.Warning);
                        return; // إيقاف الحفظ
                    }
                    // معالجة الصورة
                    string finalImageName = _currentDbImageName; // افتراضياً نبقي القديمة

                    if (_selectedImageSourcePath != null)
                    {
                        // تم اختيار صورة جديدة -> نقوم بنسخها
                        finalImageName = CopyImageToAppFolder(_selectedImageSourcePath);
                    }

                    if (_selectedId == 0)
                    {
                        // === إضافة جديد ===
                        var newProd = new Product
                        {
                            Name = name,
                            Price = price,
                            CategoryId = catId,
                            ImagePath = finalImageName,
                            IsKitchenItem = isKetchin, 
                            IsAvailable = isAvailable 
                        };
                        context.Products.Add(newProd);
                    }
                    else
                    {
                        
                        var prod = context.Products.Find(_selectedId);
                        if (prod != null)
                        {
                            prod.Name = name;
                            prod.Price = price;
                            prod.CategoryId = catId;
                            prod.IsAvailable = isAvailable;
                            prod.IsKitchenItem = isKetchin;

                            // نحدث الصورة فقط إذا تم اختيار واحدة جديدة
                            if (_selectedImageSourcePath != null)
                            {
                                prod.ImagePath = finalImageName;
                            }
                        }
                    }

                    context.SaveChanges();
                    ClearFields();
                    LoadData();
                    ToastControl.Show("معلومات", "تم الحفظ بنجاح", ToastControl.NotificationType.Success);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "error with Products page saving changes");
                ToastControl.Show("خطأ", "حدث خطأ في الحفظ", ToastControl.NotificationType.Error);
            }
        }

        
        private string CopyImageToAppFolder(string sourcePath)
        {
            try
            {
                string imagesFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "VinceApp",
                    "Images"
                );
                if (!Directory.Exists(imagesFolder))
                {
                    Directory.CreateDirectory(imagesFolder);
                }
                string extension = Path.GetExtension(sourcePath);
                string newFileName = Guid.NewGuid().ToString() + extension;
                string destPath = Path.Combine(imagesFolder, newFileName);
                File.Copy(sourcePath, destPath, false);

                return newFileName;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "error with Order page Image handling");
                ToastControl.Show("معلومات", "فشل نسخ الصورة", ToastControl.NotificationType.Info);
                return null;
            }
        }


        
        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                using (var context = new VinceSweetsDbContext())
                {
                    var prod = context.Products.Find(id);
                    if (prod != null)
                    {
                        _selectedId = prod.Id;
                        txtName.Text = prod.Name;
                        txtPrice.Text = prod.Price.ToString("0.##");
                        cmbCategories.SelectedValue = prod.CategoryId;
                        chkIsAvailable.IsChecked = prod.IsAvailable;
                        chkIsKetchin.IsChecked = prod.IsKitchenItem;
                        _currentDbImageName = prod.ImagePath; 

                        // عرض الصورة القديمة في المعاينة
                        if (!string.IsNullOrEmpty(prod.ImagePath))
                        {
                            string fullPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "VinceApp",
    "Images",
    prod.ImagePath
);

                            if (File.Exists(fullPath))
                            {
                                imgPreview.Source = new BitmapImage(new Uri(fullPath));
                                txtImageName.Text = prod.ImagePath;
                            }
                        }
                        else
                        {
                            imgPreview.Source = null;
                            txtImageName.Text = "لا توجد صورة";
                        }
                    }
                }
            }
        }

        // 5. الحذف
        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                var parentWindow = Window.GetWindow(this) as AdminWindow;

                if (parentWindow != null)
                {
                    if (await parentWindow.ShowConfirmMessage("تأكيد", "سيتم حذف المنتج هل انت متأكد؟"))
                    {
                        using (var context = new VinceSweetsDbContext())
                        {
                            var prod = context.Products.Find(id);
                            if (prod != null)
                            {
                                // قبل Remove
                                bool used = context.OrderDetails.AsNoTracking().Any(od => od.ProductId == id);

                                if (used)
                                {
                                    ToastControl.Show("لا يمكن الحذف",
                                        "هذا الصنف مستخدم في فواتير/طلبات سابقة. يمكنك تعطيله بدلاً من حذفه.",
                                        ToastControl.NotificationType.Warning);
                                    return;
                                }
                                string imageNameToDelete = prod.ImagePath;

                                context.Products.Remove(prod);
                                context.SaveChanges();

                                ToastControl.Show("معلومات", "تم الحذف بنجاح", ToastControl.NotificationType.Success);
                                try
                                {
                                    if (!string.IsNullOrWhiteSpace(imageNameToDelete))
                                    {
                                        bool usedByAnother = context.Products.Any(p => p.ImagePath == imageNameToDelete);
                                        if (!usedByAnother)
                                        {
                                            string imgFullPath = Path.Combine(
                                                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                                "VinceApp",
                                                "Images",
                                                imageNameToDelete
                                            );

                                            if (File.Exists(imgFullPath))
                                            {
                                                File.Delete(imgFullPath);
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Log.Error(ex, "error deleting product image file");
                                }
                                LoadData();
                                ClearFields();
                            }
                        }
                    }
                }
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e) => ClearFields();

        private void ClearFields()
        {
            txtName.Text = "";
            txtPrice.Text = "";
            cmbCategories.SelectedIndex = -1;
            chkIsAvailable.IsChecked = true; 
            chkIsKetchin.IsChecked = true;
            txtImageName.Text = "لم يتم اختيار صورة";
            imgPreview.Source = null;
            _selectedId = 0;
            _selectedImageSourcePath = null;
            _currentDbImageName = null;
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !decimal.TryParse(e.Text, out _); // سماح بالأرقام فقط
        }
    }
}