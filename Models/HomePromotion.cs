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

    [Range(1, 20, ErrorMessage = "จำนวนเด็กฟรีต้องอยู่ระหว่าง 1–20 คน")]
    public int MaxFreeChildren { get; set; } = 1;

    [Range(1, 18, ErrorMessage = "อายุสูงสุดต้องอยู่ระหว่าง 1–18 ปี")]
    public int MaxChildAge { get; set; } = 10;

    [Range(50, 200, ErrorMessage = "ส่วนสูงสูงสุดต้องอยู่ระหว่าง 50–200 ซม.")]
    public int MaxChildHeightCm { get; set; } = 120;
}
