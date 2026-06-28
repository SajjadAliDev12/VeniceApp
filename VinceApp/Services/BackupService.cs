using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.IO;
using System.IO.Compression; // ✅ ضروري للضغط
using System.Linq;
using System.Threading.Tasks;
using VinceApp.Data;
using VinceApp.Data.Models;
using VinceApp.Services.Cloud;

namespace VinceApp.Services
{
    public class BackupService
    {
        // تم تحويل الدالة إلى Async Task لدعم العمليات غير المتزامنة
        public async Task BackupDatabaseAsync(string userSelectedPath)
        {
            if (string.IsNullOrWhiteSpace(userSelectedPath))
                throw new Exception("مسار الحفظ غير صالح.");

            // ✅ تأكد أن مجلد الوجهة موجود
            string destDir = Path.GetDirectoryName(userSelectedPath);
            if (string.IsNullOrWhiteSpace(destDir))
                throw new Exception("مسار الحفظ غير صالح.");

            Directory.CreateDirectory(destDir);

            // ✅ تحقق أن المسار قابل للكتابة
            EnsureCanWriteFile(userSelectedPath);

            using (var context = new VinceSweetsDbContext())
            {
                var connection = context.Database.GetDbConnection();
                string dbName = connection.Database;

                // ✅ 1. نطلب من SQL Server مساره الافتراضي
                string safeBackupFolder = GetSqlDefaultBackupPath(context);

                // اسم ملف مؤقت لعملية SQL
                string tempFileName = $"Temp_{Guid.NewGuid()}.bak";
                string tempFilePath = Path.Combine(safeBackupFolder, tempFileName);

                try
                {
                    // ✅ تأكد أن خدمة SQL تقدر تكتب في هذا المجلد
                    EnsureSqlCanWriteToFolder(context, safeBackupFolder);

                    // ✅ 2. الحفظ في المجلد الآمن الخاص بالسيرفر
                    string sqlSafeTempPath = EscapeSqlPath(tempFilePath);

                    // استخدام Async هنا لتحسين الأداء
                    var command = $"BACKUP DATABASE [{dbName}] TO DISK = '{sqlSafeTempPath}' WITH FORMAT, INIT, NAME = 'Full Backup', STATS = 10;";
                    await context.Database.ExecuteSqlRawAsync(command);

                    // ✅ 3. نقل الملف لمكانك المختار (الملف الأصلي .bak)
                    if (File.Exists(tempFilePath))
                    {
                        CopyFileRobust(tempFilePath, userSelectedPath);
                    }
                    else
                    {
                        throw new Exception("تمت عملية النسخ ولكن لم يتم العثور على الملف في المسار المؤقت.");
                    }

                    // ==========================================
                    // ✅ 4. الضغط ثم الرفع السحابي ☁️📦
                    // ==========================================

                    // نحدد مسار ملف الـ Zip
                    string zipFilePath = Path.ChangeExtension(userSelectedPath, ".zip");

                    try
                    {
                        // أ) ضغط الملف
                        if (File.Exists(zipFilePath)) File.Delete(zipFilePath);

                        // إنشاء ملف Zip يحتوي على ملف الـ bak
                        using (var zip = ZipFile.Open(zipFilePath, ZipArchiveMode.Create))
                        {
                            zip.CreateEntryFromFile(userSelectedPath, Path.GetFileName(userSelectedPath));
                        }

                        // ب) رفع الملف المضغوط بدلاً من الأصلي
                        await TryUploadToCloudAsync(context, zipFilePath);
                    }
                    catch (Exception zipEx)
                    {
                        // نسجل الخطأ ولكن لا نوقف العملية لأن النسخة المحلية نجحت
                        Log.Error(zipEx, "فشل في عملية الضغط أو الرفع السحابي");
                        // يمكنك تفعيل السطر التالي إذا أردت إظهار رسالة خطأ للمستخدم رغم نجاح النسخ المحلي
                        // throw new Exception($"تم الحفظ محلياً، ولكن فشل الرفع: {zipEx.Message}");
                    }
                    finally
                    {
                        // ج) تنظيف: نحذف ملف الـ Zip المؤقت بعد الرفع
                        try
                        {
                            if (File.Exists(zipFilePath)) File.Delete(zipFilePath);
                        }
                        catch { /* تجاهل أخطاء الحذف */ }
                    }

                    // ==========================================
                    // ✅ 5. (جديد) تنظيف النسخ المحلية بذكاء 🧹
                    // ==========================================
                    try
                    {
                        // نستخرج بداية الاسم لمعرفة هل هو تلقائي أم يدوي
                        string fileName = Path.GetFileName(userSelectedPath);
                        string prefix = fileName.Split('_')[0]; // AutoBackup OR VeniceSweets

                        // نكون نمط البحث: "AutoBackup_*.bak"
                        string pattern = $"{prefix}_*.bak";

                        // التلقائي نبقي 5 نسخ، اليدوي نبقي 10
                        int keepCount = prefix.Contains("Auto") ? 5 : 10;

                        CleanupLocalBackups(destDir, pattern, keepCount);
                    }
                    catch (Exception cleanEx)
                    {
                        Log.Error(cleanEx, "Error during local cleanup logic");
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
                finally
                {
                    // ✅ 6. تنظيف الملف المؤقت الخاص بـ SQL
                    try
                    {
                        if (File.Exists(tempFilePath)) File.Delete(tempFilePath);
                    }
                    catch { /* تجاهل أخطاء الحذف */ }
                }
            }
        }

        // دالة لفحص الإعدادات والرفع والتنظيف السحابي
        private async Task TryUploadToCloudAsync(VinceSweetsDbContext context, string filePath)
        {
            try
            {
                var settings = await context.AppSettings.FirstOrDefaultAsync();

                if (settings != null &&
                    settings.AutoCloudBackup &&
                    settings.CloudBackupProvider == "Google Drive" &&
                    !string.IsNullOrEmpty(settings.CloudRefreshToken))
                {
                    var driveService = new GoogleDriveService();

                    // 1. الرفع
                    await driveService.UploadBackupAsync(filePath, settings.CloudRefreshToken);

                    // 2. التنظيف السحابي (نبقي آخر 5 نسخ)
                    await driveService.CleanupOldBackupsAsync(settings.CloudRefreshToken, 5);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "فشل الرفع السحابي التلقائي");
                throw;
            }
        }

        // دالة تنظيف النسخ المحلية (محدثة لتقبل النمط)
        private void CleanupLocalBackups(string directoryPath, string searchPattern, int maxBackupsToKeep)
        {
            try
            {
                var dir = new DirectoryInfo(directoryPath);
                if (!dir.Exists) return;

                // البحث باستخدام النمط المرسل
                var files = dir.GetFiles(searchPattern)
                               .OrderByDescending(f => f.CreationTime) // الأحدث أولاً
                               .Skip(maxBackupsToKeep) // تخطى العدد المطلوب
                               .ToList();

                foreach (var file in files)
                {
                    try
                    {
                        file.Delete();
                        Log.Information($"Deleted old local backup: {file.Name}");
                    }
                    catch { /* تجاهل */ }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error cleaning up local backups");
            }
        }

        public void RestoreDatabase(string backupFilePath)
        {
            if (string.IsNullOrWhiteSpace(backupFilePath))
                throw new Exception("مسار ملف النسخة الاحتياطية غير صالح.");

            if (!File.Exists(backupFilePath))
                throw new Exception("ملف النسخة الاحتياطية غير موجود.");

            EnsureCanReadFile(backupFilePath);

            string currentConnString = "";
            using (var context = new VinceSweetsDbContext())
            {
                currentConnString = context.Database.GetDbConnection().ConnectionString;
            }

            if (string.IsNullOrWhiteSpace(currentConnString))
                throw new Exception("فشل في العثور على نص الاتصال.");

            var builder = new SqlConnectionStringBuilder(currentConnString);
            string targetDbName = builder.InitialCatalog;
            if (string.IsNullOrEmpty(targetDbName)) targetDbName = "VinceSweetsDB";

            builder.InitialCatalog = "master";

            using (var connection = new SqlConnection(builder.ConnectionString))
            {
                connection.Open();

                using (var context = new VinceSweetsDbContext())
                {
                    string safeFolder = GetSqlDefaultBackupPath(context);
                    EnsureSqlCanWriteToFolder(context, safeFolder);

                    string tempName = $"Restore_{Guid.NewGuid()}.bak";
                    string serverBakPath = Path.Combine(safeFolder, tempName);

                    try
                    {
                        CopyFileRobust(backupFilePath, serverBakPath);

                        string sqlSafeBakPath = EscapeSqlPath(serverBakPath);
                        string safeDbName = targetDbName.Replace("]", "]]");

                        string sql = $@"
USE master;

IF EXISTS(SELECT name FROM sys.databases WHERE name = '{EscapeSqlLiteral(targetDbName)}')
BEGIN
    ALTER DATABASE [{safeDbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
END

RESTORE DATABASE [{safeDbName}]
FROM DISK = '{sqlSafeBakPath}'
WITH REPLACE, STATS = 10;

ALTER DATABASE [{safeDbName}] SET MULTI_USER;";

                        using (var command = new SqlCommand(sql, connection))
                        {
                            command.CommandTimeout = 300;
                            command.ExecuteNonQuery();
                        }
                    }
                    finally
                    {
                        try
                        {
                            if (File.Exists(serverBakPath)) File.Delete(serverBakPath);
                        }
                        catch { /* تجاهل */ }
                    }
                }
            }
        }

        // ================= Helpers =================

        private string GetSqlDefaultBackupPath(VinceSweetsDbContext context)
        {
            try
            {
                string path = string.Empty;

                using (var command = context.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = "SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS nvarchar(4000))";
                    context.Database.OpenConnection();
                    var result = command.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                        path = result.ToString();
                }

                if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
                    return path;

                string publicPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                return publicPath;
            }
            catch
            {
                return Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            }
            finally
            {
                try { context.Database.CloseConnection(); } catch { }
            }
        }

        private void EnsureSqlCanWriteToFolder(VinceSweetsDbContext context, string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
                throw new Exception("مسار مجلد السيرفر غير صالح.");

            if (!Directory.Exists(folderPath))
                throw new Exception("مجلد السيرفر غير موجود.");

            string testFile = Path.Combine(folderPath, $"_perm_{Guid.NewGuid()}.tmp");

            try
            {
                using (var fs = new FileStream(testFile, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
                {
                    fs.WriteByte(0);
                }
            }
            finally
            {
                try { if (File.Exists(testFile)) File.Delete(testFile); } catch { }
            }
        }

        private void EnsureCanReadFile(string path)
        {
            try
            {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"لا يمكن قراءة الملف: {ex.Message}");
            }
        }

        private void EnsureCanWriteFile(string path)
        {
            try
            {
                using (var fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read))
                {
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"لا يمكن الكتابة إلى المسار المحدد: {ex.Message}");
            }
        }

        private void CopyFileRobust(string source, string destination)
        {
            using (var src = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var dst = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                src.CopyTo(dst);
            }
        }

        private string EscapeSqlPath(string path)
        {
            return (path ?? "").Replace("'", "''");
        }

        private string EscapeSqlLiteral(string text)
        {
            return (text ?? "").Replace("'", "''");
        }
    }
}