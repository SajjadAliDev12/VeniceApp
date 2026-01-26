using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using VinceApp.Data;

namespace VinceApp.Services
{
    public class BackupService
    {
        public void BackupDatabase(string userSelectedPath)
        {
            using (var context = new VinceSweetsDbContext())
            {
                var connection = context.Database.GetDbConnection();
                string dbName = connection.Database;

                // ✅ 1. بدلاً من مجلد المستخدم، نطلب من SQL Server مساره الافتراضي الآمن
                string safeBackupFolder = GetSqlDefaultBackupPath(context);

                // اسم ملف مؤقت
                string tempFileName = $"Temp_{Guid.NewGuid()}.bak";
                string tempFilePath = Path.Combine(safeBackupFolder, tempFileName);

                try
                {
                    // ✅ 2. الحفظ في المجلد الآمن الخاص بالسيرفر
                    // STATS = 10 يعطي معلومات عن التقدم، FORMAT لضمان ملف جديد
                    var command = $"BACKUP DATABASE [{dbName}] TO DISK = '{tempFilePath}' WITH FORMAT, INIT, NAME = 'Full Backup';";
                    context.Database.ExecuteSqlRaw(command);

                    // ✅ 3. الآن برنامجنا (الذي يملك صلاحياتك) ينقل الملف لمكانك المختار
                    if (File.Exists(tempFilePath))
                    {
                        File.Copy(tempFilePath, userSelectedPath, true);
                    }
                    else
                    {
                        throw new Exception("تم النسخ ولكن لم يتم العثور على الملف في المسار المؤقت.");
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"خطأ أثناء النسخ: {ex.Message}");
                }
                finally
                {
                    // ✅ 4. تنظيف الملف المؤقت من مجلد السيرفر
                    try
                    {
                        if (File.Exists(tempFilePath)) File.Delete(tempFilePath);
                    }
                    catch { /* تجاهل أخطاء الحذف */ }
                }
            }
        }

        // دالة مساعدة لجلب المسار الذي يملك SQL Server صلاحية الكتابة فيه
        private string GetSqlDefaultBackupPath(VinceSweetsDbContext context)
        {
            try
            {
                // محاولة جلب مسار الباك اب الافتراضي من إعدادات السيرفر
                string path = string.Empty;

                using (var command = context.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = "SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS nvarchar(4000))";
                    context.Database.OpenConnection();
                    var result = command.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        path = result.ToString();
                    }
                }

                // إذا كان المسار موجوداً، نستخدمه
                if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
                {
                    return path;
                }

                // خطة بديلة: إذا فشل، نستخدم المجلد العام للبرامج (ProgramData) لأنه مفتوح للخدمات
                // C:\ProgramData
                string publicPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                return publicPath;
            }
            catch
            {
                // أسوأ الاحتمالات: نعود للـ C مباشرة
                return @"C:\";
            }
        }

        public void RestoreDatabase(string backupFilePath)
        {
            string currentConnString = "";

            using (var context = new VinceSweetsDbContext())
            {
                currentConnString = context.Database.GetDbConnection().ConnectionString;
            }

            if (string.IsNullOrWhiteSpace(currentConnString))
            {
                throw new Exception("فشل في العثور على نص الاتصال.");
            }

            var builder = new SqlConnectionStringBuilder(currentConnString);
            string targetDbName = builder.InitialCatalog;

            if (string.IsNullOrEmpty(targetDbName)) targetDbName = "VinceSweetsDB";

            builder.InitialCatalog = "master";

            using (var connection = new SqlConnection(builder.ConnectionString))
            {
                connection.Open();

                string sql = $@"
                    USE master;
                    
                    IF EXISTS(SELECT name FROM sys.databases WHERE name = '{targetDbName}')
                    BEGIN
                        ALTER DATABASE [{targetDbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                    END
                    
                    RESTORE DATABASE [{targetDbName}] 
                    FROM DISK = '{backupFilePath}' 
                    WITH REPLACE;

                    ALTER DATABASE [{targetDbName}] SET MULTI_USER;";

                using (var command = new SqlCommand(sql, connection))
                {
                    command.CommandTimeout = 300;
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}