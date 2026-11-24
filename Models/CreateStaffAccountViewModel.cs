using System.ComponentModel.DataAnnotations;

namespace IsanDer.Models;

public sealed class CreateStaffAccountViewModel
{
    [Required(ErrorMessage = "กรุณากรอกชื่อ-นามสกุล")]
    [StringLength(50, MinimumLength = 2)]
    [Display(Name = "ชื่อ-นามสกุล")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกอีเมล")]
    [EmailAddress(ErrorMessage = "กรุณากรอกอีเมลให้ถูกต้อง")]
    [StringLength(100)]
    [Display(Name = "อีเมล")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "กรุณากรอกเบอร์โทรศัพท์ให้ถูกต้อง")]
    [StringLength(20)]
    [Display(Name = "เบอร์โทรศัพท์ (ไม่บังคับ)")]
    public string? PhoneNumber { get; set; }

    [Required(ErrorMessage = "กรุณาเลือกตำแหน่งพนักงาน")]
    [Display(Name = "ตำแหน่ง")]
    public string Role { get; set; } = AppRoles.Staff;

    [Required(ErrorMessage = "กรุณากำหนดรหัสผ่าน")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "รหัสผ่านต้องมีความยาว 8–100 ตัวอักษร")]
    [DataType(DataType.Password)]
    [Display(Name = "รหัสผ่าน")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณายืนยันรหัสผ่าน")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "รหัสผ่านและการยืนยันรหัสผ่านไม่ตรงกัน")]
    [Display(Name = "ยืนยันรหัสผ่าน")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public sealed class CreateAccountantAccountViewModel
{
    [Required(ErrorMessage = "กรุณากรอกชื่อ-นามสกุล")]
    [StringLength(50, MinimumLength = 2)]
    [Display(Name = "ชื่อ-นามสกุล")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกอีเมล")]
    [EmailAddress(ErrorMessage = "กรุณากรอกอีเมลให้ถูกต้อง")]
    [StringLength(100)]
    [Display(Name = "อีเมล")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "กรุณากรอกเบอร์โทรศัพท์ให้ถูกต้อง")]
    [StringLength(20)]
    [Display(Name = "เบอร์โทรศัพท์ (ไม่บังคับ)")]
    public string? PhoneNumber { get; set; }

    [Required(ErrorMessage = "กรุณากำหนดรหัสผ่าน")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "รหัสผ่านต้องมีความยาว 8–100 ตัวอักษร")]
    [DataType(DataType.Password)]
    [Display(Name = "รหัสผ่าน")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณายืนยันรหัสผ่าน")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "รหัสผ่านและการยืนยันรหัสผ่านไม่ตรงกัน")]
    [Display(Name = "ยืนยันรหัสผ่าน")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
