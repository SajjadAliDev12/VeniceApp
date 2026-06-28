namespace Venice.Api.DTOs;

public sealed record SalesSummaryDto(
    decimal Today,
    decimal Week,
    decimal Month,
    int TodayOrders,
    int WeekOrders,
    int MonthOrders
);
