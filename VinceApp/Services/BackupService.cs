using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
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

                // ✅ الخطوة 1: تحديد مسار مؤقت آمن (داخل مجلد Temp الخاص بالنظام)
                // هذا المجلد مفتوح للجميع، لذا SQL Server يستطيع الكتابة فيه دائماً
                string tempFolderPath = Path.GetTempPath();
                string tempFileName = $"TempBackup_{Guid.NewGuid()}.bak";
                string tempFilePath = Path.Combine(tempFolderPath, tempFileName);

                try
                {
                    // ✅ الخطوة 2: جعل SQL Server يحفظ النسخة في المجلد المؤقت
                    var command = $"BACKUP DATABASE [{dbName}] TO DISK = '{tempFilePath}' WITH FORMAT, INIT, NAME = 'Full Backup of {dbName}';";
                    context.Database.ExecuteSqlRaw(command);

                    // ✅ الخطوة 3: برنامجك يقوم بنقل الملف من المؤقت إلى المكان الذي اختاره المستخدم
                    // (برنامجك يملك صلاحية الوصول لسطح المكتب، لذا النقل سينجح)
                    if (File.Exists(tempFilePath))
                    {
                        File.Copy(tempFilePath, userSelectedPath, true);
                    }
                    else
                    {
                        throw new Exception("لم يتم العثور على ملف النسخة الاحتياطية في المجلد المؤقت.");
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"فشل النسخ الاحتياطي: {ex.Message}");
                }
                finally
                {
                    // ✅ الخطوة 4: تنظيف (حذف الملف المؤقت)
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

            // التبديل إلى Master لعملية الاستعادة
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