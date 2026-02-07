using Serilog;
using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace VinceApp
{
    public class ImagePathConverter : IValueConverter
    {
        private static readonly string _imagesRoot =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VinceApp", "Images");

        // كاش بسيط للصور
        private static readonly ConcurrentDictionary<string, WeakReference<BitmapImage>> _imageCache = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var rawPath = value as string;
            if (string.IsNullOrEmpty(rawPath)) return null;

            try
            {
                var normalizedPath = NormalizePath(rawPath);
                if (string.IsNullOrEmpty(normalizedPath)) return null;

                // محاولة الحصول من الكاش
                if (_imageCache.TryGetValue(normalizedPath, out var weakRef) &&
                    weakRef.TryGetTarget(out var cachedImage))
                {
                    return cachedImage;
                }

                // تحميل جديد
                var image = LoadImage(normalizedPath);
                if (image != null)
                {
                    _imageCache[normalizedPath] = new WeakReference<BitmapImage>(image);
                }
                return image;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "خطأ في تحميل الصورة");
                return null;
            }
        }

        private static string NormalizePath(string rawPath)
        {
            try
            {
                // تنظيف المسار
                var cleanedPath = rawPath?.Trim();
                if (string.IsNullOrEmpty(cleanedPath)) return null;

                // التأكد من أن المسار صالح
                var invalidChars = Path.GetInvalidFileNameChars();
                foreach (var c in invalidChars)
                {
                    if (cleanedPath.Contains(c.ToString()))
                        return null;
                }

                // تجميع المسار
                var fullPath = Path.Combine(_imagesRoot, cleanedPath);

                // التحقق من أن المسار يقع داخل المجلد المطلوب (للسلامة)
                var fullPathRoot = Path.GetFullPath(fullPath);
                var imagesRootFull = Path.GetFullPath(_imagesRoot);

                if (!fullPathRoot.StartsWith(imagesRootFull, StringComparison.OrdinalIgnoreCase))
                    return null;

                return fullPathRoot;
            }
            catch
            {
                return null;
            }
        }

        private static BitmapImage LoadImage(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                    return null;

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                bitmap.DecodePixelWidth = 200;
                bitmap.DecodePixelHeight = 200;
                bitmap.UriSource = new Uri(filePath);
                bitmap.EndInit();
                bitmap.Freeze();

                return bitmap;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "فشل في تحميل الصورة: {Path}", filePath);
                return null;
            }
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

        public static void ClearCache()
        {
            _imageCache.Clear();
        }
    }
}