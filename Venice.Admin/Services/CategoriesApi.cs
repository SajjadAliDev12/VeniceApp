using System.Net.Http.Headers; // ضروري
using System.Net.Http.Json;
using Venice.Admin.Models;

namespace Venice.Admin.Services;

public record CategoryRow(int Id, string Name);

public class CategoriesApi
{
    private readonly HttpClient _http;
    public CategoriesApi(HttpClient http) => _http = http;

    public async Task<List<CategoryRow>> GetAllAsync()
    {
        // إضافة التوكن قبل الطلب
        if (!string.IsNullOrEmpty(AppSession.CurrentToken))
        {
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", AppSession.CurrentToken);
        }

        var data = await _http.GetFromJsonAsync<List<CategoryRow>>("api/Categories");
        return data ?? new();
    }
}