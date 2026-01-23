using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace VinceApp
{
    public class ImagePathConverter : IValueConverter
    {
        // ✅ مكان واحد فقط للصور: AppData\Roaming\VinceApp\Images
        private static readonly string _imagesRoot =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VinceApp", "Images");

        // ذاكرة مؤقتة (Cache) لتخزين الصور المحملة سابقاً
        private static Dictionary<string, BitmapImage> _imageCache = new Dictionary<string, BitmapImage>();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string rawPath = value as string;
            if (string.IsNullOrWhiteSpace(rawPath)) return null;

            // ✅ نثبت المسار النهائي (واحد فقط)
            string fullPath = Path.Combine(_imagesRoot, rawPath);

            // 1. فحص الذاكرة المؤقتة أولاً
            if (_imageCache.ContainsKey(fullPath))
            {
                return _imageCache[fullPath];
            }

            BitmapImage resultImage = null;

            // 2. تحميل من مجلد واحد فقط (AppData\Roaming)
            try
            {
                if (File.Exists(fullPath))
                {
                    resultImage = LoadOptimizedImage(new Uri(fullPath));
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "فشل في مسار الصور");
            }

            // 3. حفظ النتيجة في الكاش للسرعة مستقبلاً
            if (resultImage != null)
            {
                if (!_imageCache.ContainsKey(fullPath))
                {
                    _imageCache[fullPath] = resultImage;
                }
            }

            return resultImage;
        }

        // دالة التحميل السريع
        private BitmapImage LoadOptimizedImage(Uri uri)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();

            // ✅ فعّلها إذا الصور كبيرة وتظهر تدريجياً (اختياري)
            // bitmap.DecodePixelWidth = 300;

            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = uri;
            bitmap.EndInit();
            bitmap.Freeze();

            return bitmap;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        // ✅ (اختياري) استدعها مرة واحدة عند بدء البرنامج/أول حفظ صورة لضمان وجود المجلد
        public static string EnsureImagesFolder()
        {
            Directory.CreateDirectory(_imagesRoot);
            return _imagesRoot;
        }
    }
}
