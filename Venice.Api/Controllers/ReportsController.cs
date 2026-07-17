using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Venice.Api.DTOs;
using VinceApp.Data.Models;

namespace Venice.Api.Controllers;
[Authorize]
[ApiController]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly VinceSweetsDbContext _db;

    public ReportsController(VinceSweetsDbContext db) => _db = db;

    [HttpGet("sales-summary")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<SalesSummaryDto>> GetSalesSummary()
    {
        var now = DateTime.Now;
        var todayStart = now.Date;
        var weekStart = now.Date.AddDays(-7);
        var monthStart = new DateTime(now.Year, now.Month, 1);

        async Task<(decimal total, int count)> Calc(DateTime from)
        {
            // 1. تعريف الاستعلام
            var q = _db.Orders.AsNoTracking()
                 .Where(o => !o.isDeleted && o.isPaid && o.OrderDate != null && o.OrderSource != VinceApp.Data.Enums.Enums.OrderSource.EnToters && o.OrderDate >= from);

            // 2. الحساب داخل قاعدة البيانات مباشرة (بدون جلب القائمة)
            // نستخدم GroupBy وهمي لعمل Aggregation
            var result = await q
                .GroupBy(x => 1)
                .Select(g => new
                {
                    Total = g.Sum(o => (o.TotalAmount ?? 0m) - (o.DiscountAmount ?? 0m)),
                    Count = g.Count()
                })
                .FirstOrDefaultAsync();

            // إرجاع النتيجة أو أصفار في حال لم توجد طلبات
            return result != null ? (result.Total, result.Count) : (0m, 0);
        }

        var (tTotal, tCount) = await Calc(todayStart);
        var (wTotal, wCount) = await Calc(weekStart);
        var (mTotal, mCount) = await Calc(monthStart);

        return new SalesSummaryDto(tTotal, wTotal, mTotal, tCount, wCount, mCount);
    }

    [HttpGet("TotersSales-summary")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<SalesSummaryDto>> GetTotersSalesSummary()
    {
        var now = DateTime.Now;
        var todayStart = now.Date;
        var weekStart = now.Date.AddDays(-7);
        var monthStart = new DateTime(now.Year, now.Month, 1);

        async Task<(decimal total, int count)> Calc(DateTime from)
        {
            // 1. تعريف الاستعلام
            var q = _db.Orders.AsNoTracking()
                 .Where(o => !o.isDeleted && o.isPaid && o.OrderDate != null && o.OrderSource == VinceApp.Data.Enums.Enums.OrderSource.EnToters && o.OrderDate >= from);

            // 2. الحساب داخل قاعدة البيانات مباشرة (بدون جلب القائمة)
            // نستخدم GroupBy وهمي لعمل Aggregation
            var result = await q
                .GroupBy(x => 1)
                .Select(g => new
                {
                    Total = g.Sum(o => (o.TotalAmount ?? 0m) - (o.DiscountAmount ?? 0m)),
                    Count = g.Count()
                })
                .FirstOrDefaultAsync();

            // إرجاع النتيجة أو أصفار في حال لم توجد طلبات
            return result != null ? (result.Total, result.Count) : (0m, 0);
        }

        var (tTotal, tCount) = await Calc(todayStart);
        var (wTotal, wCount) = await Calc(weekStart);
        var (mTotal, mCount) = await Calc(monthStart);

        return new SalesSummaryDto(tTotal, wTotal, mTotal, tCount, wCount, mCount);
    }
}
