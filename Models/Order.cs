using System;
using System.Collections.Generic;

namespace IsanDer.Models;

public partial class Order
{
    public int OrderId { get; set; }

    public int UserId { get; set; }

    public int MenuId { get; set; }

    public string TableNumber { get; set; } = null!;

    public int Quantity { get; set; }

    public string? SpicinessLevel { get; set; }

    public string? SpecialRequest { get; set; }

    public decimal TotalAmount { get; set; }

    public string OrderStatus { get; set; } = null!;

    public DateTime? OrderDateTime { get; set; }

    public virtual Menu Menu { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
