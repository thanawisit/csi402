using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IsanDer.Models;

[Table("orders")] // ระบุให้ตรงกับชื่อตารางใน MySQL
public partial class Order
{
    [Key]
    public int OrderID { get; set; }

    public int UserID { get; set; }

    public int? TableID { get; set; }

    public DateTime OrderDate { get; set; } = DateTime.Now;

    public string OrderType { get; set; } = null!;

    public string ArriveTime { get; set; } = null!;

    public string PaymentMethod { get; set; } = null!;

    public string PaymentStatus { get; set; } = PaymentStatuses.Unpaid;

    public int? BuffetGuestCount { get; set; }

    public int BuffetFreeChildCount { get; set; }

    public string? PromoCode { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public string OrderStatus { get; set; } = "รอดำเนินการ";

    // สำหรับเชื่อมโยงกับตาราง User (เพื่อไม่ให้มี Error)
    public virtual User? User { get; set; }

    public virtual DiningTable? DiningTable { get; set; }
}