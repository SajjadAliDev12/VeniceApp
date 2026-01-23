
using System.IO;

namespace VinceApp.Services
{
    internal class FIlePathFinder
    {
        public static string GetPath(string fileName)
        {
            // 1. الحصول على مسار ملف التشغيل (.exe) الحالي
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            // 2. دمج المسار الأساسي مع المجلدات الفرعية واسم الملف
            string fullPath = Path.Combine(baseDir, "Sounds", fileName);

            return fullPath;
        }
    }
}
