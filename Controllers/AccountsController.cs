using IsanDer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace IsanDer.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class AccountsController : Controller
{
    private static readonly HashSet<string> StaffRoles = new(StringComparer.Ordinal)
    {
        AppRoles.Staff
    };

    private readonly IsanDerDbContext _context;
    private readonly IPasswordHasher<User> _passwordHasher;

    public AccountsController(IsanDerDbContext context, IPasswordHasher<User> passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public IActionResult Index()
    {
        return View(_context.Users
            .AsNoTracking()
            .OrderBy(user => user.Username)
            .Select(user => new AdminUserListItemViewModel
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role
            })
            .ToList());
    }

    [HttpGet]
    public IActionResult CreateManager()
    {
        return View(new CreateManagerAccountViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CreateManager(CreateManagerAccountViewModel model)
    {
        var email = model.Email?.Trim();
        if (!string.IsNullOrWhiteSpace(email) && _context.Users.Any(user => user.Email == email))
        {
            ModelState.AddModelError(nameof(model.Email), "อีเมลนี้มีบัญชีผู้ใช้แล้ว");
        }

        if (!ModelState.IsValid) return View(model);

        var manager = new User
        {
            Username = model.Username.Trim(),
            Email = email!,
            PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim(),
            Role = AppRoles.Manager
        };
        manager.PasswordHash = _passwordHasher.HashPassword(manager, model.Password);

        _context.Users.Add(manager);
        _context.SaveChanges();

        TempData["AdminMessage"] = "สร้างบัญชี Manager แล้ว สามารถเข้าสู่ระบบด้วยอีเมลและรหัสผ่านที่กำหนด";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult CreateStaff()
    {
        return View(new CreateStaffAccountViewModel());
    }

    [HttpGet]
    public IActionResult CreateAccountant()
    {
        return View(new CreateAccountantAccountViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CreateAccountant(CreateAccountantAccountViewModel model)
    {
        var email = model.Email?.Trim();
        if (!string.IsNullOrWhiteSpace(email) && _context.Users.Any(user => user.Email == email))
        {
            ModelState.AddModelError(nameof(model.Email), "อีเมลนี้มีบัญชีผู้ใช้แล้ว");
        }

        if (!ModelState.IsValid) return View(model);

        var accountant = new User
        {
            Username = model.Username.Trim(),
            Email = email!,
            PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim(),
            Role = AppRoles.Accountant
        };
        accountant.PasswordHash = _passwordHasher.HashPassword(accountant, model.Password);

        _context.Users.Add(accountant);
        _context.SaveChanges();

        TempData["AdminMessage"] = "สร้างบัญชีพนักงานบัญชีเรียบร้อยแล้ว";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CreateStaff(CreateStaffAccountViewModel model)
    {
        var email = model.Email?.Trim();
        if (!string.IsNullOrWhiteSpace(email) && _context.Users.Any(user => user.Email == email))
        {
            ModelState.AddModelError(nameof(model.Email), "อีเมลนี้มีบัญชีผู้ใช้แล้ว");
        }

        if (!StaffRoles.Contains(model.Role))
        {
            ModelState.AddModelError(nameof(model.Role), "กรุณาเลือกตำแหน่งพนักงานที่ถูกต้อง");
        }

        if (!ModelState.IsValid) return View(model);

        var staff = new User
        {
            Username = model.Username.Trim(),
            Email = email!,
            PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim(),
            Role = model.Role
        };
        staff.PasswordHash = _passwordHasher.HashPassword(staff, model.Password);

        _context.Users.Add(staff);
        _context.SaveChanges();

        TempData["AdminMessage"] = "สร้างบัญชีพนักงานเรียบร้อยแล้ว สามารถเข้าสู่ระบบด้วยอีเมลและรหัสผ่านที่กำหนด";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateRole(int userId, string? role)
    {
        if (role == null || !AppRoles.AssignableLabels.ContainsKey(role)) return BadRequest();

        var user = _context.Users.SingleOrDefault(account => account.UserId == userId);
        if (user == null) return NotFound();

        var currentEmail = User.FindFirstValue(ClaimTypes.Email);
        if (string.Equals(user.Email, currentEmail, StringComparison.OrdinalIgnoreCase))
        {
            TempData["AdminError"] = "ไม่สามารถเปลี่ยนบทบาทบัญชีที่กำลังใช้งานอยู่ได้";
            return RedirectToAction(nameof(Index));
        }

        user.Role = role!;
        _context.SaveChanges();
        TempData["AdminMessage"] = "เปลี่ยนบทบาทผู้ใช้เรียบร้อยแล้ว";
        return RedirectToAction(nameof(Index));
    }
}
