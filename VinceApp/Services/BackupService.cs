using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using VinceApp.Data;

namespace VinceApp.Services
{
    public class BackupService
    {
        public void BackupDatabase(string destinationPath)
        {
            using (var context = new VinceSweetsDbContext())
            {
                var dbName = context.Database.GetDbConnection().Database;
                // أمر SQL مباشر لعمل Backup
                var command = $"BACKUP DATABASE [{dbName}] TO DISK = '{destinationPath}' WITH FORMAT, MEDIANAME = 'Z_SQLServerBackups', NAME = 'Full Backup of {dbName}';";

                context.Database.ExecuteSqlRaw(command);
            }
        }

        public void RestoreDatabase(string backupFilePath)
        {
            string currentConnString = "";

            // 1. نطلب نص الاتصال من DbContext مباشرة (أضمن طريقة)
            using (var context = new VinceSweetsDbContext())
            {
                currentConnString = context.Database.GetDbConnection().ConnectionString;
            }

            if (string.IsNullOrWhiteSpace(currentConnString))
            {
                throw new Exception("فشل في العثور على نص الاتصال من النظام.");
            }

            // 2. تعديل النص للاتصال بـ master
            var builder = new SqlConnectionStringBuilder(currentConnString);
            string targetDbName = builder.InitialCatalog; 

            // تنظيف اسم قاعدة البيانات في حال كان فارغاً (لتجنب الأخطاء)
            if (string.IsNullOrEmpty(targetDbName))
            {
                targetDbName = "VinceSweetsDB";
            }

            builder.InitialCatalog = "master"; // التغيير الضروري

            // 3. التنفيذ
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