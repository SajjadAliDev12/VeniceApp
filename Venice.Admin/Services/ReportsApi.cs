using System.Net.Http.Headers; // ضروري
using System.Net.Http.Json;
using System.Text.Json;
using Venice.Admin.Models;

namespace Venice.Admin.Services;

public sealed class ReportsApi
{
    private readonly HttpClient _http;

    public ReportsApi(HttpClient http) => _http = http;

    public async Task<SalesSummaryDto?> GetSalesSummaryAsync(CancellationToken ct = default)
    {
        // إضافة التوكن يدوياً قبل الطلب
        if (!string.IsNullOrEmpty(AppSession.CurrentToken))
        {
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", AppSession.CurrentToken);
        }

        var resp = await _http.GetAsync("api/reports/sales-summary", ct);
        var body = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
        {
            // إذا كان الخطأ 401، فهذا يعني أن التوكن خطأ أو انتهت صلاحيته
            if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                throw new Exception("انتهت الجلسة، يرجى تسجيل الدخول مرة أخرى");

            throw new Exception($"HTTP {(int)resp.StatusCode} {resp.ReasonPhrase}. Body: {Trim(body)}");
        }

        // ... باقي الكود كما هو
        if (body.TrimStart().StartsWith("<"))
            throw new Exception($"HTML returned instead of JSON. Body: {Trim(body)}");

        return JsonSerializer.Deserialize<SalesSummaryDto>(
            body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );
    }

    private static string Trim(string s) => (s ?? "").Length <= 300 ? s : s[..300];
}