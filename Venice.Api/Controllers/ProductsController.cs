using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Diagnostics.Eventing.Reader;
using Venice.Api.DTOs;
using VinceApp.Data.Models;

namespace Venice.Api.Controllers;
[Authorize]
[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly VinceSweetsDbContext _db;
    public ProductsController(VinceSweetsDbContext db) => _db = db;

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> GetAll()
    {
        var data = await _db.Products.AsNoTracking()
            .Include(p => p.Category)
            .OrderBy(p => p.CategoryId).ThenBy(p => p.Name)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Price,
                p.IsAvailable,
                p.CategoryId,
                CategoryName = p.Category.Name,
                p.IsKitchenItem
            })
            .ToListAsync();

        return Ok(data);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> Update(int id, [FromBody] ProductUpdateDto dto)
    {
        var p = await _db.Products.FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return NotFound();

        p.Name = dto.Name?.Trim() ?? p.Name;
        p.Price = dto.Price;
        p.IsAvailable = dto.IsAvailable;
        p.CategoryId = dto.CategoryId;
        p.IsKitchenItem = dto.IsKitchenItem;

        await _db.SaveChangesAsync();
        return NoContent();
    }
    [HttpPost("AddProduct", Name = "AddProduct")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult> AddProduct(ProductUpdateDto product)
    {
        Product product1 = new Product
        {
            Name = product.Name,
            Price = product.Price,
            IsAvailable = product.IsAvailable,
            CategoryId = product.CategoryId,
            ImagePath = null,
            IsKitchenItem = product.IsKitchenItem,
        };

        // إضافة الكيان للمتعقب
        await _db.Products.AddAsync(product1);

        // ترحيل التغييرات فعلياً إلى قاعدة البيانات
        var result = await _db.SaveChangesAsync();

        // التحقق من أن عدد الصفوف المتأثرة أكبر من 0
        if (result > 0)
        {
            return Ok("New product has been added");
        }

        return BadRequest("Failed to save the product");
    }
    [HttpDelete("{id}", Name = "DeleteProduct")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult> DeleteProduct(int id)
    {
        // 1. البحث عن المنتج
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
        {
            return NotFound("المنتج غير موجود.");
        }

        // 2. التحقق مما إذا كان له مبيعات
        var hasOrders = await _db.OrderDetails.AnyAsync(od => od.ProductId == id);

        if (hasOrders)
        {
            // إذا كان له مبيعات، نكتفي بإخفائه من قائمة البيع
            product.IsAvailable = false;
            await _db.SaveChangesAsync();
            return Ok("تم تعطيل المنتج وإخفاؤه بدلاً من حذفه لوجود سجلات مبيعات مرتبطة به.");
        }

        // 3. إذا لم يكن له أي سجل مبيعات (منتج جديد لم يُبع أبداً)، يمكن حذفه نهائياً
        _db.Products.Remove(product);
        await _db.SaveChangesAsync();

        return Ok("تم حذف المنتج نهائياً.");
    }
}
