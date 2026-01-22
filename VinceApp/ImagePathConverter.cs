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
        // ذاكرة مؤقتة (Cache) لتخزين الصور المحملة سابقاً
        // هذا يمنع البرنامج من قراءة القرص الصلب في كل مرة تعمل فيها Scroll
        private static Dictionary<string, BitmapImage> _imageCache = new Dictionary<string, BitmapImage>();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string rawPath = value as string;

            if (string.IsNullOrWhiteSpace(rawPath)) return null;

            // 1. فحص الذاكرة المؤقتة أولاً (هل حملناها سابقاً؟)
            // نستخدم المسار كـ مفتاح، إذا موجود نرجعه فوراً دون قراءة القرص
            if (_imageCache.ContainsKey(rawPath))
            {
                return _imageCache[rawPath];
            }

            BitmapImage resultImage = null;

            // 2. محاولة التحميل من مجلد Images
            try
            {
                string appImagesFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", rawPath);
                if (File.Exists(appImagesFolder))
                {
                    resultImage = LoadOptimizedImage(new Uri(appImagesFolder));
                }
            }
            catch(Exception ex) {
                Log.Error(ex, "فشل في مسار الصور");
            }

            // 3. محاولة التحميل كمسار كامل (للقديم)
            if (resultImage == null)
            {
                try
                {
                    if (File.Exists(rawPath))
                    {
                        resultImage = LoadOptimizedImage(new Uri(rawPath));
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "فشل في مسار الصور");
                }
            }

            // 4. محاولة التحميل كـ Resource
            if (resultImage == null)
            {
                try
                {
                    resultImage = LoadOptimizedImage(new Uri(rawPath, UriKind.RelativeOrAbsolute));
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "فشل في مسار الصور");
                }
            }

            // 5. حفظ النتيجة في الكاش للسرعة مستقبلاً
            if (resultImage != null)
            {
                // إذا نجحنا في التحميل، نخزنه في الكاش
                // (نتأكد من عدم وجوده مرة أخرى تحسباً للتعددية)
                if (!_imageCache.ContainsKey(rawPath))
                {
                    _imageCache[rawPath] = resultImage;
                }
            }

            return resultImage;
        }

        // دالة التحميل السريع (السر هنا)
        private BitmapImage LoadOptimizedImage(Uri uri)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();

            // === السطر السحري للتسريع ===
            // نقول للبرنامج: لا تحمل الصورة بدقة 4K، حملها بعرض 100 بكسل فقط
            // لأننا سنعرضها في أيقونة صغيرة. هذا يقلل الحجم من 5MB إلى 5KB!
            //bitmap.DecodePixelWidth = 300;

            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = uri;
            bitmap.EndInit();

            // تجميد الصورة يجعلها أسرع في العرض ويمنع مشاكل الخيوط (Threads)
            bitmap.Freeze();

            return bitmap;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}