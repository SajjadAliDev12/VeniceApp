using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Venice.Api.DTOs;
using VinceApp.Data.Models;
using VinceApp.Services.Service;

namespace Venice.Api.Controllers;
[Authorize]
[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly VinceSweetsDbContext _db;
    public UsersController(VinceSweetsDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<UserDto>>> GetAll()
    {
        var users = await _db.Users.AsNoTracking()
            .OrderBy(u => u.Username)
            .Select(u => new UserDto(u.Id, u.Username, u.FullName, u.EmailAddress, u.Role))
            .ToListAsync();

        return users;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> Create([FromBody] UserUpsertDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
            return BadRequest("Username and Password are required.");

        var exists = await _db.Users.AnyAsync(x => x.Username == dto.Username);
        if (exists) return Conflict("Username already exists.");

        var u = new User
        {
            Username = dto.Username.Trim(),
            FullName = dto.FullName?.Trim() ?? dto.Username.Trim(),
            EmailAddress = string.IsNullOrWhiteSpace(dto.EmailAddress) ? null : dto.EmailAddress.Trim(),
            Role = dto.Role,
            PasswordHash = AuthHelper.HashText(dto.Password),
            IsEmailConfirmed = false
        };

        _db.Users.Add(u);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAll), new { id = u.Id }, null);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> Update(int id, [FromBody] UserUpsertDto dto)
    {
        var u = await _db.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (u == null) return NotFound();

        if (!string.IsNullOrWhiteSpace(dto.Username)) u.Username = dto.Username.Trim();
        u.FullName = dto.FullName?.Trim() ?? u.FullName;
        u.EmailAddress = string.IsNullOrWhiteSpace(dto.EmailAddress) ? null : dto.EmailAddress.Trim();
        u.Role = dto.Role;

        if (!string.IsNullOrWhiteSpace(dto.Password))
            u.PasswordHash = AuthHelper.HashText(dto.Password);

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> Disable(int id)
    {
        var u = await _db.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (u == null) return NotFound();

        // أفضل من الحذف: تعطيل
        u.Role = UserRole.Disabled;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
