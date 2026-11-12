using Microsoft.AspNetCore.Mvc;
using IsanDer.Models;
using System.Linq;

namespace IsanDer.Controllers
{
    public class AccountController : Controller
    {
        private readonly IsanDerDbContext _context;

        // เชื่อมต่อกับฐานข้อมูล IsanDerDbContext ของเราโดยตรง
        public AccountController(IsanDerDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // นำข้อมูลจากหน้าเว็บมาแปลงใส่ตาราง User ตามโครงสร้างของอาจารย์
                var newUser = new User
                {
                    Username = model.FullName, // เอาชื่อมาใส่เป็น Username
                    Email = model.Email,
                    PasswordHash = model.Password, // บันทึกรหัสผ่าน
                    Role = "Customer"
                };

                _context.Users.Add(newUser);
                _context.SaveChanges(); // บันทึกลง MySQL

                // เมื่อสมัครเสร็จให้กลับไปหน้าแรก
                return RedirectToAction("Index", "Home");
            }
            return View(model);
        }
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                // เช็คว่ามีอีเมลและรหัสผ่านนี้ในตาราง Users ของ MySQL หรือไม่
                var user = _context.Users.FirstOrDefault(u => u.Email == model.Email && u.PasswordHash == model.Password);

                if (user != null)
                {
                    // ถ้าข้อมูลถูกต้อง ให้เด้งไปที่หน้าแรก (Home)
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    // ถ้าไม่เจอ หรือรหัสผิด ให้แจ้งเตือน
                    ModelState.AddModelError(string.Empty, "อีเมลหรือรหัสผ่านไม่ถูกต้อง");
                }
            }
            return View(model);
        }
    }
}