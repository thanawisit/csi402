using System.ComponentModel.DataAnnotations;

namespace IsanDer.Models;

public sealed class HomePageViewModel
{
    public IReadOnlyList<HomePromotion> Promotions { get; set; } = Array.Empty<HomePromotion>();
    public IReadOnlyList<Menu> FeaturedMenus { get; set; } = Array.Empty<Menu>();
}

public sealed class PromotionManagementViewModel
{
    public HomePromotionInputViewModel Membership { get; set; } = new();
    public HomePromotionInputViewModel Buffet { get; set; } = new();
    public BuffetChildPromotionInputViewModel ChildBuffet { get; set; } = new();
}

public sealed class HomePromotionInputViewModel
{
    [Required(ErrorMessage = "กรุณากรอกข้อความกำกับ")]
    [StringLength(100)]
    public string Kicker { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกหัวข้อโปรโมชั่น")]
    [StringLength(200)]
    public string Headline { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกรายละเอียดโปรโมชั่น")]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกข้อความปุ่ม")]
    [StringLength(100)]
    public string LinkText { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}

public sealed class BuffetChildPromotionInputViewModel
{
    [StringLength(50)]
    public string? Code { get; set; }

    [DataType(DataType.Date)]
    public DateTime? StartDate { get; set; }

    [DataType(DataType.Date)]
    public DateTime? EndDate { get; set; }

    public bool IsActive { get; set; }
}
