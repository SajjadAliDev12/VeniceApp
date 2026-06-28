using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using Microsoft.Extensions.Configuration;

namespace VinceApp.Services.Cloud
{
    
    public class GoogleDriveService
    {
        

        private string ClientId = Environment.GetEnvironmentVariable("GoogleDriveClientID") ?? throw new Exception("GOOGLE_CLIENT_ID env variable is missing");
        private string ClientSecret = Environment.GetEnvironmentVariable("GoogleDriveSecretKey") ?? throw new Exception("GOOGLE_CLIENT_ID env variable is missing");
        
        private const string AppName = "VinceApp POS";
        private const string BackupFolderName = "VenicePOS_Backups"; // اسم المجلد في درايف

        private static string[] Scopes = { DriveService.Scope.DriveFile };

        // 1. الدالة القديمة (للحصول على الصلاحية أول مرة)
        public async Task<(string RefreshToken, string Email)> AuthenticateAsync()
        {
            var secrets = new ClientSecrets { ClientId = ClientId, ClientSecret = ClientSecret };
            string credPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VinceApp", "token_google");

            UserCredential credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                secrets, Scopes, "user", CancellationToken.None, new FileDataStore(credPath, true));

            var service = new DriveService(new BaseClientService.Initializer() { HttpClientInitializer = credential, ApplicationName = AppName });

            try
            {
                // التصحيح هنا: يجب تحديد الحقول لتجنب خطأ 400
                var request = service.About.Get();
                request.Fields = "user(emailAddress)";
                var about = await request.ExecuteAsync();

                // إرجاع الإيميل الحقيقي أو نص افتراضي في حال فشل الجلب
                return (credential.Token.RefreshToken, about.User.EmailAddress ?? "Google Account");
            }
            catch
            {
                // في حال فشل جلب المعلومات، نعيد التوكن فقط
                return (credential.Token.RefreshToken, "Google Account");
            }
        }


        public async Task<string> UploadBackupAsync(string filePath, string storedRefreshToken)
        {
            if (!File.Exists(filePath)) throw new FileNotFoundException("ملف النسخة الاحتياطية غير موجود");

            // أ) إعادة بناء الصلاحيات
            var credential = new UserCredential(
                new GoogleAuthorizationCodeFlow(
                    new GoogleAuthorizationCodeFlow.Initializer
                    {
                        ClientSecrets = new ClientSecrets { ClientId = ClientId, ClientSecret = ClientSecret },
                        Scopes = Scopes
                    }),
                "user",
                new TokenResponse { RefreshToken = storedRefreshToken }
            );

            // ب) إنشاء خدمة الدرايف
            var service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = AppName,
            });

            // ج) البحث عن المجلد
            string folderId = await GetOrCreateFolderAsync(service, BackupFolderName);

            // د) إعداد ملف الرفع
            var fileMetadata = new Google.Apis.Drive.v3.Data.File()
            {
                Name = Path.GetFileName(filePath),
                Parents = new List<string> { folderId }
            };

            FilesResource.CreateMediaUpload request;
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                request = service.Files.Create(fileMetadata, stream, "application/octet-stream");
                request.Fields = "id";

                // ✅ التصحيح هنا: نضع النتيجة في متغير اسمه result
                var result = await request.UploadAsync();

                // ✅ ثم نفحص الـ Status والـ Exception من المتغير result
                if (result.Status != Google.Apis.Upload.UploadStatus.Completed)
                {
                    throw new Exception($"فشل الرفع: {result.Exception?.Message}");
                }
            }

            return request.ResponseBody.Id; // إرجاع معرف الملف
        }
        public async Task CleanupOldBackupsAsync(string storedRefreshToken, int maxBackupsToKeep = 5)
        {
            try
            {
                // 1. إعداد الخدمة (نفس السابق)
                var credential = new UserCredential(
                    new GoogleAuthorizationCodeFlow(
                        new GoogleAuthorizationCodeFlow.Initializer
                        {
                            ClientSecrets = new ClientSecrets { ClientId = ClientId, ClientSecret = ClientSecret },
                            Scopes = Scopes
                        }),
                    "user",
                    new TokenResponse { RefreshToken = storedRefreshToken }
                );

                var service = new DriveService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = AppName,
                });

                string folderId = await GetOrCreateFolderAsync(service, BackupFolderName);

                // 2. إعداد طلب البحث
                var listRequest = service.Files.List();
                listRequest.Q = $"'{folderId}' in parents and trashed = false";
                listRequest.Fields = "files(id, name, createdTime)";
                listRequest.OrderBy = "createdTime desc"; // الأحدث أولاً
                listRequest.PageSize = 100; // ✅ نجبره على جلب حتى 100 ملف في الطلب الواحد

                var result = await listRequest.ExecuteAsync();
                var files = result.Files;

                // ✅ إضافة لوج لمعرفة ماذا يرى التطبيق
                Log.Information($"Google Drive CleanUp: Found {files.Count} files in backup folder.");

                if (files != null && files.Count > maxBackupsToKeep)
                {
                    var filesToDelete = files.Skip(maxBackupsToKeep).ToList();

                    Log.Information($"Files to delete: {filesToDelete.Count}");

                    foreach (var file in filesToDelete)
                    {
                        try
                        {
                            await service.Files.Delete(file.Id).ExecuteAsync();
                            Log.Information($"Deleted old cloud backup: {file.Name}");
                        }
                        catch (Exception delEx)
                        {
                            // ✅ إظهار سبب فشل الحذف في اللوج
                            Log.Error(delEx, $"Failed to delete file {file.Name} from Drive.");
                        }
                    }
                }
                else
                {
                    Log.Information("No files need to be deleted (Count <= Limit).");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Critical Error inside CleanupOldBackupsAsync");
                // لا نرمي الخطأ هنا لكي لا نوقف عملية الباك اب الناجحة، فقط نسجل المشكلة
            }
        }

        // دالة مساعدة: البحث عن المجلد أو إنشاؤه
        private async Task<string> GetOrCreateFolderAsync(DriveService service, string folderName)
        {
            // 1. بحث هل المجلد موجود؟
            var request = service.Files.List();
            request.Q = $"mimeType = 'application/vnd.google-apps.folder' and name = '{folderName}' and trashed = false";
            request.Fields = "files(id)";
            var result = await request.ExecuteAsync();

            if (result.Files != null && result.Files.Count > 0)
            {
                return result.Files[0].Id; // وجدناه
            }

            // 2. غير موجود، قم بإنشائه
            var folderMetadata = new Google.Apis.Drive.v3.Data.File()
            {
                Name = folderName,
                MimeType = "application/vnd.google-apps.folder"
            };

            var requestCreate = service.Files.Create(folderMetadata);
            requestCreate.Fields = "id";
            var folder = await requestCreate.ExecuteAsync();
            return folder.Id;
        }
    }
}