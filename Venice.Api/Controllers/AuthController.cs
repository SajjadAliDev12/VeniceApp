using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens; // مكتبة التوكن
using System.IdentityModel.Tokens.Jwt; // مكتبة التوكن
using System.Security.Claims; // مكتبة الصلاحيات
using System.Text; // للنصوص
using VinceApp.Data.Models;
using VinceApp.Services.Service;

namespace Venice.Api.Controllers;

public sealed record LoginRequest(string Username, string Password);

[Authorize] // القفل العام للكلاس
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly VinceSweetsDbContext _db;
    private readonly IConfiguration _config;
    

    public AuthController(VinceSweetsDbContext db , IConfiguration configuration)
    {
        _db = db;
        _config = configuration;
    }
    
    [HttpPost("login")]
    [AllowAnonymous] 
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var u = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Username == req.Username);

        if (u == null) return Unauthorized();

        if (u.Role == UserRole.Disabled || u.Role == UserRole.Cashier) return Unauthorized();

        if (!AuthHelper.VerifyText(req.Password, u.PasswordHash))
            return Unauthorized();

        
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_config["Venice_Secret_Key"]);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, u.Id.ToString()), 
                new Claim(ClaimTypes.Name, u.Username), 
                new Claim(ClaimTypes.Role, u.Role.ToString()) 
            }),
            Expires = DateTime.UtcNow.AddMinutes(30), 
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        // 2. إنشاء التوكن النصي
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        
        return Ok(new
        {
            Token = tokenString, 
            User = new { u.Id, u.Username, u.FullName, u.Role }
        });
    }
}