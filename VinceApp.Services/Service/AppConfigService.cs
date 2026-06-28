using System;

namespace VinceApp.Services.Service
{
    public static class AppConfigService
    {
        // نفس الاسم المستخدم في DbContext
        public static string UserConfigPath => System.IO.Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "appsettings.user.json"
        );

        // DbContext يستعملها لتحديد هل الجهاز KITCHEN أو POS
        // في الويب نخليها POS بشكل افتراضي
        public static string GetClient() => "POS";
    }
}
