using VinceApp.Data.Models;

namespace Venice.Api.DTOs;

public sealed record UserDto(int Id, string Username, string FullName, string? EmailAddress, UserRole Role);

public sealed class UserUpsertDto
{
    public string Username { get; set; } = "";
    public string FullName { get; set; } = "";
    public string? EmailAddress { get; set; }
    public UserRole Role { get; set; } = UserRole.Cashier;

    // في الإضافة/تغيير كلمة السر فقط
    public string? Password { get; set; }
}
