using System;
using System.Collections.Generic;

namespace VinceApp.Data;

public partial class Product
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public decimal Price { get; set; }

    public string? ImagePath { get; set; }

    public bool? IsKitchenItem { get; set; }

    public int? CategoryId { get; set; }

    public virtual Category? Category { get; set; }
    public bool IsAvailable { get; set; }

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
}
