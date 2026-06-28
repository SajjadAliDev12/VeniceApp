using Serilog;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using VinceApp.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace VinceApp.Services
{
    public class AutoBackupScheduler
    {
        private static bool _isRunning = false;
        private static DateTime _lastBackupDate = DateTime.MinValue;

        // الوقت المحدد للنسخ التلقائي (مثلاً 2:00 صباحاً)
        // يمكنك جعل هذا الرقم متغيراً في الإعدادات لاحقاً
        private const int BackupHour = 21;

        public async void Start()
        {
            if (_isRunning) return;
            _isRunning = true;

            Log.Information("تم تشغيل خدمة النسخ الاحتياطي التلقائي...");

            // حلقة لا نهائية تعمل طالما البرنامج مفتوح
            while (_isRunning)
            {
                try
                {
                    // 1. التحقق: هل الساعة الآن هي ساعة النسخ؟ وهل لم نقم بالنسخ اليوم؟
                    if (DateTime.Now.Hour == BackupHour && _lastBackupDate.Date != DateTime.Now.Date)
                    {
                        await PerformAutoBackup();
                        _lastBackupDate = DateTime.Now.Date; // سجل أننا انتهينا لليوم
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error in AutoBackup Scheduler loop");
                }

                // انتظر دقيقة قبل الفحص التالي (60000 ميلي ثانية)
                await Task.Delay(60000);
            }
        }

        public void Stop()
        {
            _isRunning = false;
        }

        // دالة تنفذ النسخ الفعلي (بدون حوارات UI)
        public async Task PerformAutoBackup()
        {
            try
            {
                using (var context = new VinceSweetsDbContext())
                {
                    var settings = await context.AppSettings.FirstOrDefaultAsync();

                    // تحقق هل النسخ التلقائي مفعل؟
                    if (settings == null || !settings.AutoCloudBackup) return;

                    Log.Information("بدء عملية النسخ الاحتياطي التلقائي...");

                    // تحديد المسار الافتراضي (مثلاً مجلد المستندات/VinceBackups)
                    string docsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    string backupFolder = Path.Combine(docsPath, "VinceApp_AutoBackups");

                    if (!Directory.Exists(backupFolder)) Directory.CreateDirectory(backupFolder);

                    // اسم الملف (يتضمن التاريخ والوقت)
                    string fileName = $"AutoBackup_{DateTime.Now:yyyyMMdd_HHmm}.bak";
                    string fullPath = Path.Combine(backupFolder, fileName);

                    // استدعاء خدمة النسخ الموجودة لدينا
                    var backupService = new BackupService();
                    await backupService.BackupDatabaseAsync(fullPath);

                    Log.Information("✅ تمت عملية النسخ الاحتياطي التلقائي والرفع بنجاح.");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "❌ فشل النسخ الاحتياطي التلقائي");
            }
        }
    }
}