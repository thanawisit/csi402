using System.ComponentModel.DataAnnotations;

namespace IsanDer.Models;

public sealed class HomePageViewModel
{
    public IReadOnlyList<HomePromotion> Promotions { get; set; } = Array.Empty<HomePromotion>();
    public IReadOnlyList<GeneralPromotion> GeneralPromotions { get; set; } = Array.Empty<GeneralPromotion>();
    public IReadOnlyList<Menu> FeaturedMenus { get; set; } = Array.Empty<Menu>();
    public BuffetChildPromotion? CurrentBuffetPromotion { get; set; }
}

public sealed class PromotionManagementViewModel
{
    public HomePromotionInputViewModel Membership { get; set; } = new();
    public HomePromotionInputViewModel Buffet { get; set; } = new();
    public BuffetChildPromotionInputViewModel ChildBuffet { get; set; } = new();
    public IReadOnlyList<HomePromotion> ExtraPromotions { get; set; } = Array.Empty<HomePromotion>();
    public IReadOnlyList<GeneralPromotion> GeneralPromotions { get; set; } = Array.Empty<GeneralPromotion>();
}

public sealed class GeneralPromotionInputViewModel
{
    [Required(ErrorMessage = "กรุณากรอกโค้ดโปรโมชั่น")]
    [StringLength(50, MinimumLength = 3)]
    [Display(Name = "โค้ดโปรโมชั่น")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกชื่อโปรโมชั่น")]
    [StringLength(200)]
    [Display(Name = "ชื่อโปรโมชั่น")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "รายละเอียด")]
    public string? Description { get; set; }

    [Required]
    [Display(Name = "ประเภทส่วนลด")]
    public string DiscountType { get; set; } = "Percent";

    [Range(typeof(decimal), "0.01", "100000", ErrorMessage = "มูลค่าต้องมากกว่า 0")]
    [Display(Name = "มูลค่าคูปอง")]
    public decimal DiscountValue { get; set; }

    [Required]
    [Display(Name = "ใช้กับ")]
    public string AppliesTo { get; set; } = "Both";

    [Display(Name = "เฉพาะเดือนเกิด")]
    public bool BirthMonthOnly { get; set; }

    [Display(Name = "ใช้ได้เฉพาะออเดอร์แรก")]
    public bool FirstOrderOnly { get; set; }

    [Range(1, 100000, ErrorMessage = "จำนวนครั้งต่อสมาชิกต้องมากกว่า 0")]
    [Display(Name = "จำนวนครั้งต่อสมาชิก")]
    public int? MaxUsesPerUser { get; set; }

    [Display(Name = "เมนูที่ได้รับฟรี")]
    public int? TargetMenuId { get; set; }

    [Display(Name = "ต้องพนักงานยืนยันแต่งกายตามธีม")]
    public bool RequiresThemeApproval { get; set; }

    [Range(typeof(decimal), "0", "100000", ErrorMessage = "ยอดขั้นต่ำต้องไม่น้อยกว่า 0")]
    [Display(Name = "ยอดขั้นต่ำ")]
    public decimal MinSubtotal { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "วันที่เริ่ม")]
    public DateTime? StartDate { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "วันที่สิ้นสุด")]
    public DateTime? EndDate { get; set; }

    [Range(1, 100000, ErrorMessage = "จำนวนใช้ต้องมากกว่า 0")]
    [Display(Name = "จำนวนครั้งสูงสุด")]
    public int? MaxUses { get; set; }

    [Display(Name = "เปิดใช้งาน")]
    public bool IsActive { get; set; } = true;
}

public sealed class PromotionCreateViewModel
{
    [Required(ErrorMessage = "กรุณากรอกข้อความกำกับ")]
    [StringLength(100)]
    [Display(Name = "ข้อความกำกับ")]
    public string Kicker { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกชื่อโปรโมชั่น")]
    [StringLength(200)]
    [Display(Name = "ชื่อโปรโมชั่น")]
    public string Headline { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกรายละเอียดโปรโมชั่น")]
    [StringLength(500)]
    [Display(Name = "รายละเอียด")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกข้อความปุ่ม")]
    [StringLength(100)]
    [Display(Name = "ข้อความปุ่ม")]
    public string LinkText { get; set; } = "ดูรายละเอียด";

    [Display(Name = "เปิดใช้งาน")]
    public bool IsActive { get; set; } = true;
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

    [Range(1, 20)]
    [Display(Name = "จำนวนเด็กฟรีสูงสุด")]
    public int MaxFreeChildren { get; set; } = 1;

    [Range(1, 18)]
    [Display(Name = "อายุเด็กสูงสุด")]
    public int MaxChildAge { get; set; } = 10;

    [Range(50, 200)]
    [Display(Name = "ส่วนสูงเด็กสูงสุด (ซม.)")]
    public int MaxChildHeightCm { get; set; } = 120;
}
