namespace Venice.Api.DTOs;

public sealed class ProductUpdateDto
{
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public bool IsAvailable { get; set; }
    public int CategoryId { get; set; }
    public bool IsKitchenItem { get; set; }
}
