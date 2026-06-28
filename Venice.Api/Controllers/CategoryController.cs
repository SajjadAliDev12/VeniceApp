using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Venice.Api.DTOs;
using VinceApp.Data.Models;

namespace Venice.Api.Controllers
{
    [Authorize]
    [Route("api/Categories")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly VinceSweetsDbContext _db;
        public CategoryController(VinceSweetsDbContext db)
        {
            _db = db;
        }
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task <ActionResult> GetCategories()
        {
            var data = await _db.Categories.AsNoTracking().OrderBy(c => c.Id).ThenBy(c => c.Name).Select(c=> new {c.Id,c.Name} ).ToListAsync();
            if (data != null)
            {
                return Ok(data);
            }
            else return NotFound();
        }
    }
}
