using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IsanDer.Models;

[Table("order_items")]
public partial class OrderItem
{
    [Key]
    public int OrderItemID { get; set; }
    public int OrderID { get; set; }
    public int MenuID { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public bool StockDeducted { get; set; }
    [StringLength(500)]
    public string? SpecialRequest { get; set; }
}