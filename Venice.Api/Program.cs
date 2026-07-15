using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Reflection;
using System.Text;
using VinceApp.Data.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. تثبيت الرابط على المنفذ 5000 (HTTP) لضمان أن الـ Blazor يجده دائماً
builder.WebHost.UseUrls("http://*:5000");

// 2. إضافة خدمة الـ CORS (مهمة جداً للاتصال بين التطبيقات)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<VinceSweetsDbContext>(opt =>
{
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

VinceSweetsDbContext.EnableAudit(false);
var SecretKey = builder.Configuration["Venice_Secret_Key"];
if(string.IsNullOrEmpty(SecretKey))
{
    throw new Exception("secret key is not configured.");
}
var key = Encoding.ASCII.GetBytes(SecretKey);
builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});
builder.Host.UseWindowsService();

var app = builder.Build();

// تفعيل الـ CORS
app.UseCors("AllowAll");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection(); // ممتاز أنك أوقفت هذا
app.UseAuthentication();
app.UseAuthorization();
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapControllers();

app.MapFallbackToFile("index.html");
try
{
    app.Run();
}
catch (Exception ex) // جعلت الـ Catch عاماً ليلتقط أي خطأ
{
    Console.WriteLine($"Critical Error: {ex.Message}");
    throw;
}