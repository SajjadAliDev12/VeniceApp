using Venice.Admin.Models;
namespace Venice.Admin.Models;
public static class AppSession
{
    public static string? CurrentToken { get; set; } // هنا سنخزن المفتاح
}

// كلاس لاستقبال رد السيرفر الجديد
public class LoginResponse
{
    public string Token { get; set; }
    public UserRow User { get; set; }
}