using System;
using Microsoft.AspNetCore.Mvc;
using IsanDer.Models;
using System.Linq;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;

namespace IsanDer.Controllers
{
    public class AccountController : Controller
    {
        private readonly IsanDerDbContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;

        // เชื่อมต่อกับฐานข้อมูล IsanDerDbContext ของเราโดยตรง
        public AccountController(IsanDerDbContext context, IPasswordHasher<User> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        [HttpGet]
        public IActionResult Register(string? returnUrl)
        {
            return View(new RegisterViewModel
            {
                ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(RegisterViewModel model)
        {
            if (!model.DateOfBirth.HasValue)
            {
                ModelState.AddModelError(nameof(model.DateOfBirth), "กรุณากรอกวันเกิด");
            }
            else if (model.DateOfBirth.Value.Date > DateTime.Today)
            {
                ModelState.AddModelError(nameof(model.DateOfBirth), "วันเกิดต้องไม่เป็นวันที่ในอนาคต");
            }

            if (_context.Users.Any(user => user.Email == model.Email))
            {
                ModelState.AddModelError(nameof(model.Email), "อีเมลนี้มีบัญชีผู้ใช้แล้ว");
            }

            if (!ModelState.IsValid) return View(model);

            var newUser = new User
            {
                Username = model.FullName.Trim(),
                Email = model.Email.Trim(),
                PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim(),
                DateOfBirth = model.DateOfBirth?.Date,
                MarketingConsent = model.MarketingConsent,
                TermsAcceptedAt = DateTime.UtcNow,
                Role = AppRoles.Customer
            };
            newUser.PasswordHash = _passwordHasher.HashPassword(newUser, model.Password);

            _context.Users.Add(newUser);
            _context.SaveChanges();

            if (Url.IsLocalUrl(model.ReturnUrl))
            {
                return RedirectToAction(nameof(Login), new { returnUrl = model.ReturnUrl });
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl)
        {
            return View(new LoginViewModel
            {
                ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null
            });
        }

        // *** เปลี่ยนให้รองรับ async Task เพื่อสร้าง Cookie ***
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!Url.IsLocalUrl(model.ReturnUrl))
            {
                model.ReturnUrl = null;
            }

            if (ModelState.IsValid)
            {
                var user = _context.Users.FirstOrDefault(u => u.Email == model.Email);

                if (user != null && VerifyAndUpgradePassword(user, model.Password!))
                {
                    // 1. สร้างข้อมูลประจำตัว (Claims) ที่จะฝังไว้ใน Cookie
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                        new Claim(ClaimTypes.Name, user.Username),
                        new Claim(ClaimTypes.Email, user.Email),
                        new Claim(ClaimTypes.Role, user.Role ?? "Customer")
                    };

                    // 2. ระบุรูปแบบการยืนยันตัวตนว่าเป็นแบบ Cookie
                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    // 3. สั่งให้เบราว์เซอร์สร้าง Cookie และจดจำการ Login
                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme, 
                        new ClaimsPrincipal(claimsIdentity));

                    if (user.Role != AppRoles.Customer)
                    {
                        return RedirectToAction(
                            user.Role == AppRoles.Manager ? "Manager" : "Index",
                            "Admin");
                    }

                    return model.ReturnUrl != null
                        ? LocalRedirect(model.ReturnUrl)
                        : RedirectToAction("Index", "Home");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "อีเมลหรือรหัสผ่านไม่ถูกต้อง");
                }
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        private bool VerifyAndUpgradePassword(User user, string password)
        {
            PasswordVerificationResult result;
            try
            {
                result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
            }
            catch (FormatException)
            {
                result = PasswordVerificationResult.Failed;
            }

            if (result == PasswordVerificationResult.Failed)
            {
                if (!string.Equals(user.PasswordHash, password, StringComparison.Ordinal)) return false;
                result = PasswordVerificationResult.SuccessRehashNeeded;
            }

            if (result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash = _passwordHasher.HashPassword(user, password);
                _context.SaveChanges();
            }

            return true;
        }
    }
}