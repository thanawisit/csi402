using System.ComponentModel.DataAnnotations;

namespace IsanDer.Models;

public sealed class OrderEditViewModel
{
    [Range(1, int.MaxValue)]
    public int OrderId { get; set; }

    public string OrderStatus { get; set; } = string.Empty;

    public string PaymentStatus { get; set; } = string.Empty;

    public decimal CurrentTotalAmount { get; set; }

    public List<OrderEditLineViewModel> Items { get; set; } = new();

    public List<OrderMenuOptionViewModel> Menus { get; set; } = new();
}

public sealed class OrderEditLineViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "กรุณาเลือกรายการอาหาร")]
    public int MenuId { get; set; }

    [Range(1, 99, ErrorMessage = "จำนวนต้องอยู่ระหว่าง 1 ถึง 99")]
    public int Quantity { get; set; }
}

public sealed class OrderMenuOptionViewModel
{
    public int MenuId { get; set; }

    public string MenuName { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public string Status { get; set; } = string.Empty;

    public int? StockQuantity { get; set; }
}

public sealed class OrderListItemViewModel
{
    public int OrderId { get; set; }

    public string MenuName { get; set; } = string.Empty;

    public int Quantity { get; set; }
}
