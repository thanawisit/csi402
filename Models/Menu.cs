using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IsanDer.Models;

public partial class Menu
{
    public int MenuId { get; set; }

    public string MenuName { get; set; } = null!;

    public decimal Price { get; set; }

    public string Status { get; set; } = null!;

    public int? StockQuantity { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    [NotMapped]
    public string? ImagePath { get; set; }
}