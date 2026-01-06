using System;
using System.Collections.Generic;

namespace VinceApp.Data;

public partial class OrderDetail
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public int ProductId { get; set; }

    public string ProductName { get; set; } = null!;

    public int Quantity { get; set; }

    public decimal Price { get; set; }
    public bool IsServed { get; set; } // هل انتهى المطبخ من تحضيره؟
    public virtual Order Order { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
