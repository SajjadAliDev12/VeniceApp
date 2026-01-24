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
            return AppConfigService.GetActivatedFlag();
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
