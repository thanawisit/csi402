using IsanDer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace IsanDer.Controllers;

[Authorize]
public class AdminController : Controller
{
    private static readonly string[] MenuStatuses = ["Available", "Unavailable"];
    private static readonly string[] AllOrderStatuses =
    [
        "รอดำเนินการ",
        "ยืนยันแล้ว",
        "กำลังเตรียม",
        "พร้อมรับ",
        "เสร็จแล้ว",
        "ยกเลิก"
    ];

    private readonly IsanDerDbContext _context;

    public AdminController(IsanDerDbContext context)
    {
        _context = context;
    }

    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.Manager + "," + AppRoles.Receptionist + "," + AppRoles.Chef + "," + AppRoles.StockChecker)]
    public IActionResult Index()
    {
        if (User.IsInRole(AppRoles.Manager))
        {
            return RedirectToAction(nameof(Manager));
        }

        var dashboard = BuildDashboard();
        return User.IsInRole(AppRoles.Admin)
            ? View("AdminDashboard", dashboard)
            : View(dashboard);
    }

    [Authorize(Roles = AppRoles.Manager)]
    public IActionResult Manager()
    {
        return View(BuildDashboard());
    }

    private AdminDashboardViewModel BuildDashboard()
    {
        var canManageMenus = User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.Manager);
        var canManageOrders = User.IsInRole(AppRoles.Admin) ||
                              User.IsInRole(AppRoles.Receptionist) ||
                              User.IsInRole(AppRoles.Chef);
        var canManageStock = User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.StockChecker);
        var canManageUsers = canManageMenus;
        var isManager = User.IsInRole(AppRoles.Manager);
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        var todayOrders = _context.Orders
            .AsNoTracking()
            .Where(order => order.OrderDate >= today && order.OrderDate < tomorrow);

        return new AdminDashboardViewModel
        {
            MenuCount = canManageMenus || canManageStock ? _context.Menus.Count() : 0,
            PendingOrderCount = canManageOrders
                ? _context.Orders.Count(order => order.OrderStatus == "รอดำเนินการ")
                : 0,
            UserCount = canManageUsers
                ? isManager
                    ? _context.Users.Count(user => user.Role != AppRoles.Customer)
                    : _context.Users.Count()
                : 0,
            StaffCount = canManageUsers && !isManager
                ? _context.Users.Count(user => user.Role != AppRoles.Customer)
                : 0,
            CustomerCount = canManageUsers && !isManager
                ? _context.Users.Count(user => user.Role == AppRoles.Customer)
                : 0,
            TodayOrderCount = canManageOrders ? todayOrders.Count() : 0,
            TodayOrderTotal = canManageOrders
                ? todayOrders.Select(order => (decimal?)order.TotalAmount).Sum() ?? 0m
                : 0m,
            RecentOrders = canManageOrders
                ? _context.Orders
                    .AsNoTracking()
                    .Include(order => order.User)
                    .Include(order => order.DiningTable)
                    .OrderByDescending(order => order.OrderDate)
                    .Take(8)
                    .ToList()
                : Array.Empty<Order>()
        };
    }

    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.Manager)]
    public IActionResult Menus()
    {
        return View(_context.Menus.AsNoTracking().OrderBy(menu => menu.MenuName).ToList());
    }

    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.Manager)]
    public IActionResult MenuCreate()
    {
        ViewBag.MenuStatuses = MenuStatuses;
        return View("MenuForm", new MenuInputViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.Manager)]
    public IActionResult MenuCreate(MenuInputViewModel model)
    {
        if (!MenuStatuses.Contains(model.Status))
        {
            ModelState.AddModelError(nameof(model.Status), "สถานะเมนูไม่ถูกต้อง");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.MenuStatuses = MenuStatuses;
            return View("MenuForm", model);
        }

        _context.Menus.Add(new Menu
        {
            MenuName = model.MenuName.Trim(),
            Price = model.Price,
            Status = model.Status
        });
        _context.SaveChanges();

        TempData["AdminMessage"] = "เพิ่มเมนูเรียบร้อยแล้ว";
        return RedirectToAction(nameof(Menus));
    }

    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.Manager)]
    public IActionResult MenuEdit(int id)
    {
        var menu = _context.Menus.AsNoTracking().SingleOrDefault(item => item.MenuId == id);
        if (menu == null) return NotFound();

        ViewBag.MenuStatuses = MenuStatuses;
        return View("MenuForm", new MenuInputViewModel
        {
            MenuId = menu.MenuId,
            MenuName = menu.MenuName,
            Price = menu.Price,
            Status = menu.Status
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.Manager)]
    public IActionResult MenuEdit(MenuInputViewModel model)
    {
        if (!model.MenuId.HasValue) return BadRequest();
        if (!MenuStatuses.Contains(model.Status))
        {
            ModelState.AddModelError(nameof(model.Status), "สถานะเมนูไม่ถูกต้อง");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.MenuStatuses = MenuStatuses;
            return View("MenuForm", model);
        }

        var menu = _context.Menus.SingleOrDefault(item => item.MenuId == model.MenuId.Value);
        if (menu == null) return NotFound();

        menu.MenuName = model.MenuName.Trim();
        menu.Price = model.Price;
        menu.Status = model.Status;
        _context.SaveChanges();

        TempData["AdminMessage"] = "บันทึกเมนูเรียบร้อยแล้ว";
        return RedirectToAction(nameof(Menus));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.Manager)]
    public IActionResult DisableMenu(int id)
    {
        var menu = _context.Menus.SingleOrDefault(item => item.MenuId == id);
        if (menu == null) return NotFound();

        menu.Status = "Unavailable";
        _context.SaveChanges();
        TempData["AdminMessage"] = "ปิดการขายเมนูแล้ว ข้อมูลเดิมยังอยู่ในประวัติออเดอร์";
        return RedirectToAction(nameof(Menus));
    }

    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.Receptionist + "," + AppRoles.Chef)]
    public IActionResult Orders()
    {
        ViewBag.AllowedOrderStatuses = GetAllowedOrderStatuses();
        return View(_context.Orders
            .AsNoTracking()
            .Include(order => order.User)
            .Include(order => order.DiningTable)
            .OrderByDescending(order => order.OrderDate)
            .ToList());
    }

    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.Manager)]
    public IActionResult Reports(DateTime? fromDate, DateTime? toDate)
    {
        var today = DateTime.Today;
        if (!ModelState.IsValid)
        {
            return View(new SalesReportViewModel
            {
                StartDate = new DateTime(today.Year, today.Month, 1),
                EndDate = today,
                ErrorMessage = "รูปแบบวันที่ไม่ถูกต้อง"
            });
        }

        var startDate = fromDate?.Date ?? new DateTime(today.Year, today.Month, 1);
        var endDate = toDate?.Date ?? today;
        if (startDate > endDate || endDate == DateTime.MaxValue.Date)
        {
            return View(new SalesReportViewModel
            {
                StartDate = startDate,
                EndDate = endDate,
                ErrorMessage = startDate > endDate
                    ? "วันที่เริ่มต้นต้องไม่อยู่หลังวันที่สิ้นสุด"
                    : "วันที่สิ้นสุดไม่ถูกต้อง"
            });
        }

        var endExclusive = endDate.AddDays(1);
        var completedOrders = _context.Orders
            .AsNoTracking()
            .Where(order =>
                order.OrderStatus == "เสร็จแล้ว" &&
                order.OrderDate >= startDate &&
                order.OrderDate < endExclusive);
        var completedOrderCount = completedOrders.Count();
        var totalSales = completedOrders
            .Select(order => (decimal?)order.TotalAmount)
            .Sum() ?? 0m;

        return View(new SalesReportViewModel
        {
            StartDate = startDate,
            EndDate = endDate,
            CompletedOrderCount = completedOrderCount,
            TotalSales = totalSales,
            AverageOrderAmount = completedOrderCount == 0 ? 0m : totalSales / completedOrderCount,
            CompletedOrders = completedOrders
                .Include(order => order.User)
                .Include(order => order.DiningTable)
                .OrderByDescending(order => order.OrderDate)
                .Take(50)
                .ToList()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.Receptionist + "," + AppRoles.Chef)]
    public IActionResult UpdateOrderStatus(int id, string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return BadRequest();

        using var transaction = _context.Database.BeginTransaction();
        var order = _context.Orders.SingleOrDefault(item => item.OrderID == id);
        if (order == null) return NotFound();
        if (order.OrderStatus == status) return RedirectToAction(nameof(Orders));
        if (!GetAllowedOrderStatuses().Contains(status)) return Forbid();
        if (order.OrderStatus == "ยกเลิก" && status != "ยกเลิก")
        {
            TempData["AdminError"] = "ออเดอร์ที่ยกเลิกแล้วไม่สามารถกลับมาเปิดใหม่ได้";
            return RedirectToAction(nameof(Orders));
        }
        if (order.OrderStatus == "เสร็จแล้ว" && status == "ยกเลิก")
        {
            TempData["AdminError"] = "ไม่สามารถยกเลิกออเดอร์ที่เสร็จแล้ว";
            return RedirectToAction(nameof(Orders));
        }

        if (status == "ยกเลิก" && order.OrderStatus != "ยกเลิก")
        {
            var orderItems = _context.OrderItems
                .Where(item => item.OrderID == order.OrderID && item.StockDeducted)
                .ToList();
            foreach (var item in orderItems)
            {
                var menu = _context.Menus.SingleOrDefault(existingMenu => existingMenu.MenuId == item.MenuID);
                if (menu?.StockQuantity is int stockQuantity)
                {
                    menu.StockQuantity = stockQuantity + item.Quantity;
                }
                item.StockDeducted = false;
            }
        }

        order.OrderStatus = status;
        _context.SaveChanges();
        transaction.Commit();

        TempData["AdminMessage"] = "อัปเดตสถานะออเดอร์แล้ว";
        return RedirectToAction(nameof(Orders));
    }

    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.StockChecker)]
    public IActionResult Stock()
    {
        return View(_context.Menus
            .AsNoTracking()
            .OrderBy(menu => menu.MenuName)
            .Select(menu => new StockMenuViewModel
            {
                MenuId = menu.MenuId,
                MenuName = menu.MenuName,
                StockQuantity = menu.StockQuantity,
                Status = menu.Status
            })
            .ToList());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.StockChecker)]
    public IActionResult UpdateStock(int id, int? stockQuantity, string? status)
    {
        if (stockQuantity < 0 || !MenuStatuses.Contains(status ?? string.Empty))
        {
            return BadRequest();
        }

        var menu = _context.Menus.SingleOrDefault(item => item.MenuId == id);
        if (menu == null) return NotFound();

        menu.StockQuantity = stockQuantity;
        menu.Status = status!;
        _context.SaveChanges();

        TempData["AdminMessage"] = "บันทึกสต๊อกและสถานะเมนูแล้ว";
        return RedirectToAction(nameof(Stock));
    }

    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.Manager)]
    public IActionResult Users()
    {
        var canManageRoles = User.IsInRole(AppRoles.Admin);
        ViewBag.CanManageRoles = canManageRoles;
        ViewBag.IsManager = User.IsInRole(AppRoles.Manager);
        var users = _context.Users.AsNoTracking();
        if (!canManageRoles && User.IsInRole(AppRoles.Manager))
        {
            users = users.Where(user => user.Role != AppRoles.Customer);
        }

        return View(users
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Admin)]
    public IActionResult UpdateUserRole(int userId, string? role)
    {
        if (!AppRoles.IsValid(role)) return BadRequest();

        var user = _context.Users.SingleOrDefault(account => account.UserId == userId);
        if (user == null) return NotFound();

        var currentEmail = User.FindFirstValue(ClaimTypes.Email);
        if (string.Equals(user.Email, currentEmail, StringComparison.OrdinalIgnoreCase))
        {
            TempData["AdminError"] = "ไม่สามารถเปลี่ยนบทบาทบัญชีที่กำลังใช้งานอยู่ได้";
            return RedirectToAction(nameof(Users));
        }

        user.Role = role!;
        _context.SaveChanges();
        TempData["AdminMessage"] = "เปลี่ยนบทบาทผู้ใช้เรียบร้อยแล้ว";
        return RedirectToAction(nameof(Users));
    }

    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }

    private string[] GetAllowedOrderStatuses()
    {
        if (User.IsInRole(AppRoles.Admin))
        {
            return AllOrderStatuses;
        }

        if (User.IsInRole(AppRoles.Chef))
        {
            return ["กำลังเตรียม", "พร้อมรับ"];
        }

        if (User.IsInRole(AppRoles.Receptionist))
        {
            return ["ยืนยันแล้ว", "เสร็จแล้ว", "ยกเลิก"];
        }

        return [];
    }
}
