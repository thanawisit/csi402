using System;
using System.ComponentModel.DataAnnotations;

namespace IsanDer.Models
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "กรุณากรอกอีเมล")]
        [EmailAddress]
        [Display(Name = "อีเมล")]
        public string Email { get; set; }

        [Required(ErrorMessage = "กรุณากรอกชื่อ-นามสกุล")]
        [Display(Name = "ชื่อ-นามสกุล")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "กรุณากรอกวันเกิด (สำหรับรับโปรวันเกิด)")]
        [DataType(DataType.Date)]
        [Display(Name = "วันเกิด")]
        public DateTime DateOfBirth { get; set; }

        [Required(ErrorMessage = "กรุณากรอกรหัสผ่าน")]
        [DataType(DataType.Password)]
        [Display(Name = "รหัสผ่าน")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "ยืนยันรหัสผ่าน")]
        [Compare("Password", ErrorMessage = "รหัสผ่านและการยืนยันรหัสผ่านไม่ตรงกัน")]
        public string ConfirmPassword { get; set; }
    }
}