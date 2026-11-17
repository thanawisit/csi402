using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace IsanDer.Models
{
    public class RegisterViewModel : IValidatableObject
    {
        [Required(ErrorMessage = "กรุณากรอกอีเมล")]
        [EmailAddress]
        [StringLength(100)]
        [Display(Name = "อีเมล")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "กรุณากรอกชื่อ-นามสกุล")]
        [StringLength(50, MinimumLength = 2)]
        [Display(Name = "ชื่อ-นามสกุล")]
        public string FullName { get; set; } = string.Empty;

        [Phone(ErrorMessage = "กรุณากรอกเบอร์โทรศัพท์ให้ถูกต้อง")]
        [StringLength(20)]
        [Display(Name = "เบอร์โทรศัพท์ (ไม่บังคับ)")]
        public string? PhoneNumber { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "วันเกิด (ไม่บังคับ)")]
        public DateTime? DateOfBirth { get; set; }

        [Required(ErrorMessage = "กรุณากรอกรหัสผ่าน")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "รหัสผ่านต้องมีความยาว 8–100 ตัวอักษร")]
        [DataType(DataType.Password)]
        [Display(Name = "รหัสผ่าน")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "กรุณายืนยันรหัสผ่าน")]
        [DataType(DataType.Password)]
        [Display(Name = "ยืนยันรหัสผ่าน")]
        [Compare("Password", ErrorMessage = "รหัสผ่านและการยืนยันรหัสผ่านไม่ตรงกัน")]
        public string ConfirmPassword { get; set; } = string.Empty;

        public bool TermsAccepted { get; set; }

        [Display(Name = "ยินยอมรับข่าวสารและโปรโมชัน")]
        public bool MarketingConsent { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!TermsAccepted)
            {
                yield return new ValidationResult(
                    "กรุณายอมรับข้อกำหนดการใช้บริการและรับทราบนโยบายความเป็นส่วนตัว",
                    new[] { nameof(TermsAccepted) });
            }

            if (DateOfBirth.HasValue && DateOfBirth.Value.Date > DateTime.Today)
            {
                yield return new ValidationResult(
                    "วันเกิดต้องไม่เป็นวันที่ในอนาคต",
                    new[] { nameof(DateOfBirth) });
            }
        }
    }
}