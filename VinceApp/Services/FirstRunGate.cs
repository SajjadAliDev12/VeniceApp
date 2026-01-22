using Serilog;
using System;
using System.Windows;
using VinceApp.Data;

namespace VinceApp.Services
{
    public static class FirstRunGate
    {
        public static bool IsReady()
        {
            // 1) لازم التفعيل يكون true
            if (!AppConfigService.GetActivatedFlag())
                return false;

            // 2) لازم نقدر نتصل بالقاعدة
            try
            {
                using var ctx = new VinceSweetsDbContext();
                return ctx.Database.CanConnect();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "FirstRunGate: DB connection failed");
                return false;
            }
        }

        public static bool RunWizardIfNeeded()
        {
            if (IsReady()) return true;

            // افتح نافذة أول تشغيل
            var w = new FirstRunWizardWindow();
            bool? ok = w.ShowDialog();
            return ok == true;
        }
    }
}
