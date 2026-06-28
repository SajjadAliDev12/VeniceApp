using System.Net.Http.Headers; // ضروري للهيدر
using System.Net.Http.Json;
using Venice.Admin.Models;

namespace Venice.Admin.Services;

public class UsersApi
{
    private readonly HttpClient _http;

    public UsersApi(HttpClient http)
    {
        _http = http;
    }

    // دالة مساعدة صغيرة لإضافة التوكن قبل كل طلب
    private void AddTokenHeader()
    {
        if (!string.IsNullOrEmpty(AppSession.CurrentToken))
        {
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", AppSession.CurrentToken);
        }
    }

    // جلب كل المستخدمين
    public async Task<List<UserRow>> GetAllAsync()
    {
        AddTokenHeader(); // <--- ركب المفتاح أولاً
        var list = await _http.GetFromJsonAsync<List<UserRow>>("api/users");
        return list ?? new List<UserRow>();
    }

    // إضافة مستخدم
    public async Task CreateAsync(UserUpsertDto dto)
    {
        AddTokenHeader(); // <--- ركب المفتاح
        var response = await _http.PostAsJsonAsync("api/users", dto);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"فشل الإضافة: {error}");
        }
    }

    // تعديل مستخدم
    public async Task UpdateAsync(int id, UserUpsertDto dto)
    {
        AddTokenHeader(); // <--- ركب المفتاح
        var response = await _http.PutAsJsonAsync($"api/users/{id}", dto);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"فشل التعديل: {error}");
        }
    }

    // تعطيل مستخدم
    public async Task DisableAsync(int id)
    {
        AddTokenHeader(); // <--- ركب المفتاح
        var response = await _http.DeleteAsync($"api/users/{id}");

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"فشل التعطيل: {error}");
        }
    }

    public record LoginRequest(string Username, string Password);

    // --- التغيير الكبير هنا ---
    public async Task<UserRow?> LoginAsync(string username, string password)
    {
        // تسجيل الدخول لا يحتاج توكن (AllowAnonymous)
        var response = await _http.PostAsJsonAsync("api/auth/login", new LoginRequest(username, password));

        if (response.IsSuccessStatusCode)
        {
            // نقرأ الرد بالشكل الجديد (توكن + يوزر)
            var result = await response.Content.ReadFromJsonAsync<LoginResponse>();

            if (result != null)
            {
                // 1. نحفظ التوكن في الذاكرة لنستخدمه لاحقاً
                AppSession.CurrentToken = result.Token;

                // 2. نرجع بيانات المستخدم للواجهة
                return result.User;
            }
        }

        return null;
    }
}