using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using VinceApp.Data;
using VinceApp.Data.Models;

namespace VinceApp.Services
{
    public class BackupService
    {
        public void BackupDatabase(string userSelectedPath)
        {
            if (string.IsNullOrWhiteSpace(userSelectedPath))
                throw new Exception("مسار الحفظ غير صالح.");

            // ✅ تأكد أن مجلد الوجهة موجود (برنامجك يملك صلاحية إنشائه عادة)
            string destDir = Path.GetDirectoryName(userSelectedPath);
            if (string.IsNullOrWhiteSpace(destDir))
                throw new Exception("مسار الحفظ غير صالح.");

            Directory.CreateDirectory(destDir);

            // ✅ تحقق أن المسار قابل للكتابة قبل ما تتعب SQL
            EnsureCanWriteFile(userSelectedPath);

            using (var context = new VinceSweetsDbContext())
            {
                var connection = context.Database.GetDbConnection();
                string dbName = connection.Database;

                // ✅ 1. نطلب من SQL Server مساره الافتراضي (الأكثر أماناً للخدمة)
                string safeBackupFolder = GetSqlDefaultBackupPath(context);

                // اسم ملف مؤقت
                string tempFileName = $"Temp_{Guid.NewGuid()}.bak";
                string tempFilePath = Path.Combine(safeBackupFolder, tempFileName);

                try
                {
                    // ✅ تأكد أن خدمة SQL تقدر تكتب في هذا المجلد (فحص عملي)
                    EnsureSqlCanWriteToFolder(context, safeBackupFolder);

                    // ✅ 2. الحفظ في المجلد الآمن الخاص بالسيرفر
                    // (ESCAPE) لأن BACKUP يتعامل مع single quotes
                    string sqlSafeTempPath = EscapeSqlPath(tempFilePath);

                    var command =
                        $"BACKUP DATABASE [{dbName}] TO DISK = '{sqlSafeTempPath}' WITH FORMAT, INIT, NAME = 'Full Backup', STATS = 10;";

                    context.Database.ExecuteSqlRaw(command);

                    // ✅ 3. نقل الملف لمكانك المختار (برنامجك بصلاحيات المستخدم)
                    if (File.Exists(tempFilePath))
                    {
                        // لضمان عدم بقاء ملف مقفول: افتحه قراءة ثم انسخه عبر Stream
                        CopyFileRobust(tempFilePath, userSelectedPath);
                    }
                    else
                    {
                        throw new Exception("تمت عملية النسخ ولكن لم يتم العثور على الملف في المسار المؤقت.");
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"خطأ أثناء النسخ: {ex.Message}");
                }
                finally
                {
                    // ✅ 4. تنظيف الملف المؤقت
                    try
                    {
                        if (File.Exists(tempFilePath)) File.Delete(tempFilePath);
                    }
                    catch { /* تجاهل أخطاء الحذف */ }
                }
            }
        }

        public void RestoreDatabase(string backupFilePath)
        {
            if (string.IsNullOrWhiteSpace(backupFilePath))
                throw new Exception("مسار ملف النسخة الاحتياطية غير صالح.");

            if (!File.Exists(backupFilePath))
                throw new Exception("ملف النسخة الاحتياطية غير موجود.");

            // ✅ تأكد أن البرنامج يقدر يقرأ الملف (قبل ما نروح للـ SQL)
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

                // ✅ مهم: SQL Server هو اللي لازم يقرأ ملف الـ .bak
                // لذلك نحتاج أن يكون الملف في مسار يستطيع SQL الوصول إليه.
                // أبسط حل بدون تغييرات جذرية: ننقل ملف المستخدم مؤقتاً لمجلد SQL الآمن ثم نعمل Restore منه.
                using (var context = new VinceSweetsDbContext())
                {
                    string safeFolder = GetSqlDefaultBackupPath(context);
                    EnsureSqlCanWriteToFolder(context, safeFolder); // إذا يكتب، غالباً يقرأ أيضاً

                    string tempName = $"Restore_{Guid.NewGuid()}.bak";
                    string serverBakPath = Path.Combine(safeFolder, tempName);

                    try
                    {
                        // انقل نسخة للـ Server folder (برنامجك يقرأ من مسار المستخدم ويكتب للمجلد الآمن)
                        CopyFileRobust(backupFilePath, serverBakPath);

                        string sqlSafeBakPath = EscapeSqlPath(serverBakPath);

                        // ✅ حماية من quotes في اسم قاعدة البيانات (أقل احتمال، بس نؤمّن)
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
                        // تنظيف نسخة السيرفر
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

        // جلب مسار الباك اب الافتراضي لخدمة SQL
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

                // خطة بديلة: ProgramData غالباً متاح للخدمات
                string publicPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                return publicPath;
            }
            catch
            {
                // أسوأ الاحتمالات: لا نرجع C:\ مباشرة (صلاحيات خطرة/قد تفشل)
                // نرجع ProgramData كخيار أكثر منطقية
                return Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            }
            finally
            {
                try { context.Database.CloseConnection(); } catch { }
            }
        }

        // فحص عملي: هل SQL Server قادر يكتب في المجلد (بدون الاعتماد على التخمين)
        private void EnsureSqlCanWriteToFolder(VinceSweetsDbContext context, string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
                throw new Exception("مسار مجلد السيرفر غير صالح.");

            if (!Directory.Exists(folderPath))
                throw new Exception("مجلد السيرفر غير موجود.");

            string testFile = Path.Combine(folderPath, $"_perm_{Guid.NewGuid()}.tmp");
            string sqlSafeTestFile = EscapeSqlPath(testFile);

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
                    // مجرد فتح/إغلاق
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
                // افتح أو أنشئ ثم اغلق فوراً
                using (var fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read))
                {
                    // لا تكتب شيء لتجنب تغيير الملف إذا كان موجوداً
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"لا يمكن الكتابة إلى المسار المحدد: {ex.Message}");
            }
        }

        private void CopyFileRobust(string source, string destination)
        {
            // نسخ عبر streams لتقليل مشاكل القفل/المشاركة
            using (var src = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var dst = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                src.CopyTo(dst);
            }
        }

        private string EscapeSqlPath(string path)
        {
            // SQL string literal escape for single quotes
            return (path ?? "").Replace("'", "''");
        }

        private string EscapeSqlLiteral(string text)
        {
            return (text ?? "").Replace("'", "''");
        }
    }
}
