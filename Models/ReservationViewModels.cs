using System.ComponentModel.DataAnnotations;

namespace IsanDer.Models;

public sealed class ReservationDetailsViewModel
{
    [Range(1, int.MaxValue)]
    public int TableId { get; set; }

    [Required]
    public string Package { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณาเลือกวันที่ต้องการจอง")]
    [DataType(DataType.Date)]
    [Display(Name = "วันที่ต้องการจอง")]
    public DateTime? ReservationDate { get; set; }

    [Required(ErrorMessage = "กรุณาเลือกเวลาที่ต้องการจอง")]
    [DataType(DataType.Time)]
    [Display(Name = "เวลาที่ต้องการจอง")]
    public TimeOnly? ReservationTime { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "กรุณาระบุจำนวนผู้ใช้บริการ")]
    [Display(Name = "จำนวนผู้ใช้บริการ")]
    public int GuestCount { get; set; } = 1;

    [Required(ErrorMessage = "กรุณากรอกชื่อผู้จอง")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "ชื่อต้องมีความยาว 2–50 ตัวอักษร")]
    [Display(Name = "ชื่อผู้จอง")]
    public string CustomerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกเบอร์โทรศัพท์")]
    [Phone(ErrorMessage = "กรุณากรอกเบอร์โทรศัพท์ให้ถูกต้อง")]
    [StringLength(20)]
    [Display(Name = "เบอร์โทรศัพท์")]
    public string CustomerPhone { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "หมายเหตุต้องไม่เกิน 500 ตัวอักษร")]
    [Display(Name = "หมายเหตุเพิ่มเติม")]
    public string? Note { get; set; }

    public string CustomerEmail { get; set; } = string.Empty;

    public string TableNumber { get; set; } = string.Empty;

    public int TableCapacity { get; set; }
}

public sealed class ReservationAdminListItemViewModel
{
    public int ReservationId { get; set; }
    public string TableNumber { get; set; } = string.Empty;
    public DateTime ReservationDateTime { get; set; }
    public int GuestCount { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string? Note { get; set; }
    public string Package { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public sealed class ReservationConfirmationViewModel
{
    public int ReservationId { get; set; }
    public int TableId { get; set; }
    public string TableNumber { get; set; } = string.Empty;
    public DateTime ReservationDateTime { get; set; }
    public int GuestCount { get; set; }
    public string Package { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public sealed class ReservationHistoryItemViewModel
{
    public int ReservationId { get; set; }
    public int TableId { get; set; }
    public string TableNumber { get; set; } = string.Empty;
    public DateTime ReservationDateTime { get; set; }
    public int GuestCount { get; set; }
    public string Package { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public sealed class ReservationDetailsDisplayViewModel
{
    public int ReservationId { get; set; }
    public int TableId { get; set; }
    public string TableNumber { get; set; } = string.Empty;
    public DateTime ReservationDateTime { get; set; }
    public int GuestCount { get; set; }
    public string Package { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string? Note { get; set; }
}
