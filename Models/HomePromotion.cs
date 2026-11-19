using System.ComponentModel.DataAnnotations;

namespace IsanDer.Models;

public sealed class HomePromotion
{
    public int PromotionId { get; set; }

    [Required]
    [StringLength(40)]
    public string SlotKey { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Kicker { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Headline { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string LinkText { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; }
}

public sealed class BuffetChildPromotion
{
    public byte PromotionId { get; set; } = 1;

    [StringLength(50)]
    public string? Code { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public bool IsActive { get; set; }
}
