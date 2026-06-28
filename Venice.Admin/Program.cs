using Venice.Admin.Services;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://*:5002");
// --- 1. إصلاح مشكلة الكوكيز مع HTTP ---
// هذا السطر ضروري جداً ليقبل المتصفح حفظ تسجيل الدخول بدون شهادة حماية
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // يعمل مع HTTP و HTTPS
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// إضافة خدمات الـ Blazor
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// تحديد رابط الـ API
// ملاحظة: تأكد أن خدمة الـ API تعمل فعلياً على هذا البورت (في الـ Publish غالباً يكون 5000 وليس 7001 إلا إذا غيرته يدوياً)
var apiUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5000";

// تسجيل الخدمات
builder.Services.AddHttpClient<ReportsApi>(client => { client.BaseAddress = new Uri(apiUrl); });
builder.Services.AddHttpClient<ProductsApi>(client => { client.BaseAddress = new Uri(apiUrl); });
builder.Services.AddHttpClient<CategoriesApi>(client => { client.BaseAddress = new Uri(apiUrl); });
builder.Services.AddHttpClient<UsersApi>(client => { client.BaseAddress = new Uri(apiUrl); });

builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, Venice.Admin.Services.CustomAuthStateProvider>();
builder.Host.UseWindowsService();
builder.Services.AddCors(options => {
    options.AddDefaultPolicy(policy => {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

// --- 2. إيقاف HSTS و HTTPS Redirection ---
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // app.UseHsts(); // <--- تم الإيقاف: يسبب مشاكل عند عدم وجود SSL
}

app.UseCors();
// app.UseHttpsRedirection(); // <--- تم الإيقاف: هذا هو سبب فشل الاتصال الرئيسي

app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();