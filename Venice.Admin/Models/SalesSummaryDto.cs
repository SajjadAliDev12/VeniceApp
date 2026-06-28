namespace Venice.Admin.Models;

public sealed class SalesSummaryDto
{
    public decimal Today { get; set; }
    public decimal Week { get; set; }
    public decimal Month { get; set; }

    public int TodayOrders { get; set; }
    public int WeekOrders { get; set; }
    public int MonthOrders { get; set; }
}
