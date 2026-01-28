using Serilog;
using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace VinceApp
{
    public class ImagePathConverter : IValueConverter
    {
        // مكان الصور الثابت
        private static readonly string _imagesRoot =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VinceApp", "Images");

        // تم حذف _imageCache المسبب للتسريب

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string rawPath = value as string;
            if (string.IsNullOrWhiteSpace(rawPath)) return null;

            string fullPath = Path.Combine(_imagesRoot, rawPath);

            try
            {
                if (File.Exists(fullPath))
                {
                    // تحميل الصورة مباشرة بدون تخزينها في متغير static
                    return LoadOptimizedImage(new Uri(fullPath));
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "فشل في مسار الصور");
            }

            return null;
        }

        private BitmapImage LoadOptimizedImage(Uri uri)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();

            // تحجيم الصورة لتوفير الذاكرة (اختياري لكن مفيد جداً في قوائم المنتجات)
            // إذا كانت الصور صغيرة في العرض، لا داعي لتحميلها بدقة 4K مثلاً
            bitmap.DecodePixelWidth = 200;

            bitmap.CacheOption = BitmapCacheOption.OnLoad; // تحميل فوري لفك القفل عن الملف
            bitmap.UriSource = uri;
            bitmap.EndInit();
            bitmap.Freeze(); // تجميد الصورة لجعلها Thread-Safe ولزيادة الأداء

            return bitmap;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public static string EnsureImagesFolder()
        {
            Directory.CreateDirectory(_imagesRoot);
            return _imagesRoot;
        }
    }
}