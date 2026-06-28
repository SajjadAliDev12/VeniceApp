using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using System.Security.Claims;
using Venice.Admin.Models;

namespace Venice.Admin.Services;

// كلاس صغير لتخزين الجلسة كاملة (التوكن + المستخدم) داخل المتصفح
public class UserSession
{
    public string Token { get; set; }
    public UserRow User { get; set; }
}

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly ProtectedSessionStorage _sessionStorage;
    private ClaimsPrincipal _anonymous = new(new ClaimsIdentity());

    public CustomAuthStateProvider(ProtectedSessionStorage sessionStorage)
    {
        _sessionStorage = sessionStorage;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            // محاولة جلب الجلسة (التي تحتوي التوكن واليوزر)
            var result = await _sessionStorage.GetAsync<UserSession>("UserSession");
            var session = result.Success ? result.Value : null;

            if (session == null || session.User == null)
                return new AuthenticationState(_anonymous);

            // هام جداً: استرجاع التوكن ووضعه في AppSession
            // لكي تستطيع الـ APIs استخدامه
            AppSession.CurrentToken = session.Token;

            var identity = CreateIdentity(session.User);
            return new AuthenticationState(new ClaimsPrincipal(identity));
        }
        catch
        {
            return new AuthenticationState(_anonymous);
        }
    }

    // تم تعديل الدالة لتستقبل التوكن أيضاً
    public async Task MarkUserAsAuthenticated(UserRow user, string token)
    {
        // 1. تحديث المتغير العام فوراً
        AppSession.CurrentToken = token;

        // 2. إنشاء كائن الجلسة وحفظه في المتصفح
        var session = new UserSession { User = user, Token = token };
        await _sessionStorage.SetAsync("UserSession", session);

        // 3. تحديث حالة التطبيق
        var identity = CreateIdentity(user);
        var claimsPrincipal = new ClaimsPrincipal(identity);

        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(claimsPrincipal)));
    }

    public async Task MarkUserAsLoggedOut()
    {
        // مسح التوكن
        AppSession.CurrentToken = null;
        await _sessionStorage.DeleteAsync("UserSession");

        var claimsPrincipal = new ClaimsPrincipal(_anonymous);
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(claimsPrincipal)));
    }

    private ClaimsIdentity CreateIdentity(UserRow user)
    {
        return new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        }, "Cookies");
    }
}