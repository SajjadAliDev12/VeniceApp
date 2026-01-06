using System;
using System.Collections.Generic;

namespace VinceApp.Data;

public partial class Order
{
    public int Id { get; set; }

    public int OrderNumber { get; set; }

    public DateTime? OrderDate { get; set; }

    public decimal? TotalAmount { get; set; }

    public string? OrderStatus { get; set; }
    
    public int? TableId { get; set; }

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual RestaurantTable? Table { get; set; }
}
