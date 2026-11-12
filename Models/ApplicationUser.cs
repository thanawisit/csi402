using Microsoft.AspNetCore.Identity;
using System;

namespace IsanDer.Models
{
    // สืบทอดจาก IdentityUser เพื่อใช้ระบบ Login ของ ASP.NET Core
    public class ApplicationUser : IdentityUser 
    {
        public string FullName { get; set; }
        public DateTime DateOfBirth { get; set; } 
        public bool IsMember { get; set; } 
    }
}