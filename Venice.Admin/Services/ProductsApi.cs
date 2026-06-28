using System.Net.Http.Headers; // ضروري للهيدر
using System.Net.Http.Json;
using Venice.Admin.Models;

namespace Venice.Admin.Services;

public sealed class ProductsApi
{
    private readonly HttpClient _http;

    public ProductsApi(HttpClient http) => _http = http;

    // دالة مساعدة لإضافة التوكن
    private void AddTokenHeader()
    {
        // نأخذ التوكن من الكلاس العام الذي انشاناه سابقاً
        if (!string.IsNullOrEmpty(AppSession.CurrentToken))
        {
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", AppSession.CurrentToken);
        }
    }

    public async Task<List<ProductRow>> GetAllAsync(CancellationToken ct = default)
    {
        AddTokenHeader(); // <--- ركب التوكن
        var data = await _http.GetFromJsonAsync<List<ProductRow>>("api/products", ct);
        return data ?? new();
    }

    public async Task UpdateAsync(int id, ProductUpdateDto dto, CancellationToken ct = default)
    {
        AddTokenHeader(); // <--- ركب التوكن
        var resp = await _http.PutAsJsonAsync($"api/products/{id}", dto, ct);
        resp.EnsureSuccessStatusCode();
    }

    public async Task CreateAsync(ProductUpdateDto dto)
    {
        AddTokenHeader(); // <--- ركب التوكن
        var response = await _http.PostAsJsonAsync("api/Products/AddProduct", dto);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception(error);
        }
    }

    public async Task DeleteAsync(int id)
    {
        AddTokenHeader(); // <--- ركب التوكن
        var response = await _http.DeleteAsync($"api/Products/{id}");

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception(error);
        }
    }
}

// الموديلات كما هي (تأكد من وجودها في ملفاتها أو هنا)
public sealed class ProductRow
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public bool IsAvailable { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = "";
    public bool IsKitchenItem { get; set; }
}

public sealed class ProductUpdateDto
{
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public bool IsAvailable { get; set; }
    public int CategoryId { get; set; }
    public bool IsKitchenItem { get; set; }
}