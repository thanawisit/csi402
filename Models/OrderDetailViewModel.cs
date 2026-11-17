namespace IsanDer.Models;

public class OrderDetailViewModel
{
    public Order Order { get; set; } = null!;

    public List<OrderDetailItemViewModel> Items { get; set; } = new();
}

public class OrderDetailItemViewModel
{
    public string MenuName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal LineTotal => UnitPrice * Quantity;
}
