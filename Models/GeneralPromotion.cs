using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IsanDer.Models;

[Table("general_promotions")]
public sealed class GeneralPromotion
{
    [Key]
    public int PromotionId { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string DiscountType { get; set; } = "Percent";

    public decimal DiscountValue { get; set; }

    public string AppliesTo { get; set; } = "Both";

    public bool BirthMonthOnly { get; set; }

    public bool FirstOrderOnly { get; set; }

    public int? MaxUsesPerUser { get; set; }

    public int? TargetMenuId { get; set; }

    public bool RequiresThemeApproval { get; set; }

    public decimal MinSubtotal { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public int? MaxUses { get; set; }

    public int UsedCount { get; set; }

    public bool IsActive { get; set; } = true;
}
