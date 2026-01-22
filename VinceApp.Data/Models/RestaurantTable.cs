using System;
using System.Collections.Generic;

namespace VinceApp.Data;

public partial class RestaurantTable
{
    public int Id { get; set; }

    public int TableNumber { get; set; }

    public string? TableName { get; set; }

    public int? Status { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
