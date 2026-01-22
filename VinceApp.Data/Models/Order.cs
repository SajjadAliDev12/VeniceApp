using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VinceApp.Data;

public partial class Order
{
    public int Id { get; set; }

    public int OrderNumber { get; set; }

    public DateTime? OrderDate { get; set; }

    public decimal? TotalAmount { get; set; }

    public int? ParentOrderId { get; set; }
    public int? TableId { get; set; }
    public bool isReady { get; set; }
    public bool isServed { get; set; }
    public bool isSentToKitchen { get; set; }
    public bool isPaid { get; set; }
    [Required]
    public bool isDeleted { get; set; } = false;
    public decimal DiscountAmount { get; set; } = 0;
    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual RestaurantTable? Table { get; set; }

    [NotMapped]
    public string StatusText
    {
        get
        {
            if (isServed) return "تم التسليم";
            if (isReady) return "جاهز للاستلام"; 
            if (isPaid) return "مدفوع";
            if (isSentToKitchen) return "قيد التحضير";
            return "مفتوح";
        }
    }
}
