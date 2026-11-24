using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IsanDer.Models;

[Table("general_promotion_usages")]
public sealed class GeneralPromotionUsage
{
    [Key]
    public int UsageId { get; set; }

    public int PromotionId { get; set; }

    public int UserId { get; set; }

    public int OrderId { get; set; }

    public DateTime UsedAt { get; set; } = DateTime.Now;

    public bool RequiresThemeApproval { get; set; }

    public string ThemeApprovalStatus { get; set; } = string.Empty;
}
