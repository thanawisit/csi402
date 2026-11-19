using IsanDer.Models;
using IsanDer.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

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
    private readonly IWebHostEnvironment _environment;

    public AdminController(IsanDerDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
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
            : View("~/Views/Staff/Index.cshtml", dashboard);
    }

    [Authorize(Roles = AppRoles.Manager)]
    public IActionResult Manager()
    {
        return View("~/Views/Manager/Index.cshtml", BuildDashboard());
    }

    private AdminDashboardViewModel BuildDashboard()
    {
        var canManageMenus = User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.Manager);
        var canManageOrders = User.IsInRole(AppRoles.Admin) ||
                              User.IsInRole(AppRoles.Manager) ||
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
            TodayPaidTotal = canManageOrders
                ? todayOrders
                    .Where(order => order.PaymentStatus == PaymentStatuses.Paid)
                    .Select(order => (decimal?)order.TotalAmount)
                    .Sum() ?? 0m
                : 0m,
            PendingPaymentCount = canManageOrders
                ? _context.Orders.Count(order =>
                    order.PaymentStatus == PaymentStatuses.PendingReview ||
                    order.PaymentStatus == PaymentStatuses.NeedsResubmission)
                : 0,
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
        var menus = _context.Menus.AsNoTracking().OrderBy(menu => menu.MenuName).ToList();
        foreach (var menu in menus)
        {
            menu.ImagePath = MenuImageStorage.GetPublicPath(_environment.WebRootPath, menu.MenuId);
        }

        return BackOfficeView(
            "Menus",
            menus);
    }

    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.Manager)]
    public IActionResult MenuCreate()
    {
        ViewBag.MenuStatuses = MenuStatuses;
        return BackOfficeView("MenuForm", new MenuInputViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.Manager)]
    public async Task<IActionResult> MenuCreate(MenuInputViewModel model, CancellationToken cancellationToken)
    {
        if (!MenuStatuses.Contains(model.Status))
        {
            ModelState.AddModelError(nameof(model.Status), "สถานะเมนูไม่ถูกต้อง");
        }

        var hasImage = model.ImageFile is { Length: > 0 };
        var imageExtension = string.Empty;
        if (hasImage && !MenuImageStorage.TryGetExtension(model.ImageFile, out imageExtension))
        {
            ModelState.AddModelError(nameof(model.ImageFile), "กรุณาเลือกรูป JPG หรือ PNG ขนาดไม่เกิน 5 MB");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.MenuStatuses = MenuStatuses;
            return BackOfficeView("MenuForm", model);
        }

        var menu = new Menu
        {
            MenuName = model.MenuName.Trim(),
            Price = model.Price,
            Status = model.Status
        };
        _context.Menus.Add(menu);
        _context.SaveChanges();

        if (hasImage)
        {
            await MenuImageStorage.SaveAsync(
                _environment.WebRootPath,
                menu.MenuId,
                model.ImageFile!,
                imageExtension,
                cancellationToken);
        }

        TempData["AdminMessage"] = "เพิ่มเมนูเรียบร้อยแล้ว";
        return RedirectToAction(nameof(Menus));
    }

    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.Manager)]
    public IActionResult MenuEdit(int id)
    {
        var menu = _context.Menus.AsNoTracking().SingleOrDefault(item => item.MenuId == id);
        if (menu == null) return NotFound();

        ViewBag.MenuStatuses = MenuStatuses;
        return BackOfficeView("MenuForm", new MenuInputViewModel
        {
            MenuId = menu.MenuId,
            MenuName = menu.MenuName,
            Price = menu.Price,
            Status = menu.Status,
            CurrentImagePath = MenuImageStorage.GetPublicPath(_environment.WebRootPath, menu.MenuId)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.Manager)]
    public async Task<IActionResult> MenuEdit(MenuInputViewModel model, CancellationToken cancellationToken)
    {
        if (!model.MenuId.HasValue) return BadRequest();
        var menu = _context.Menus.SingleOrDefault(item => item.MenuId == model.MenuId.Value);
        if (menu == null) return NotFound();

        if (!MenuStatuses.Contains(model.Status))
        {
            ModelState.AddModelError(nameof(model.Status), "สถานะเมนูไม่ถูกต้อง");
        }

        var hasImage = model.ImageFile is { Length: > 0 };
        var imageExtension = string.Empty;
        if (hasImage && !MenuImageStorage.TryGetExtension(model.ImageFile, out imageExtension))
        {
            ModelState.AddModelError(nameof(model.ImageFile), "กรุณาเลือกรูป JPG หรือ PNG ขนาดไม่เกิน 5 MB");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.MenuStatuses = MenuStatuses;
            model.CurrentImagePath = MenuImageStorage.GetPublicPath(_environment.WebRootPath, menu.MenuId);
            return BackOfficeView("MenuForm", model);
        }

        menu.MenuName = model.MenuName.Trim();
        menu.Price = model.Price;
        menu.Status = model.Status;
        _context.SaveChanges();

        if (hasImage)
        {
            await MenuImageStorage.SaveAsync(
                _environment.WebRootPath,
                menu.MenuId,
                model.ImageFile!,
                imageExtension,
                cancellationToken);
        }

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

    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.Manager)]
    [HttpGet]
    public IActionResult Promotions()
    {
        var promotions = _context.HomePromotions.AsNoTracking().ToDictionary(promotion => promotion.SlotKey);
        var childBuffet = _context.BuffetChildPromotions
            .AsNoTracking()
            .Single(promotion => promotion.PromotionId == 1);

        return BackOfficeView("Promotions", new PromotionManagementViewModel
        {
            Membership = ToPromotionInput(promotions["membership"]),
            Buffet = ToPromotionInput(promotions["buffet"]),
            ChildBuffet = new BuffetChildPromotionInputViewModel
            {
                Code = childBuffet.Code,
                StartDate = childBuffet.StartDate,
                EndDate = childBuffet.EndDate,
                IsActive = childBuffet.IsActive
            }
        });
    }

    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.Manager)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Promotions(PromotionManagementViewModel model)
    {
        var code = model.ChildBuffet.Code?.Trim();
        if (model.ChildBuffet.IsActive)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                ModelState.AddModelError("ChildBuffet.Code", "กรุณากำหนดรหัสโปรโมชั่นก่อนเปิดใช้");
            }
            if (!model.ChildBuffet.StartDate.HasValue || !model.ChildBuffet.EndDate.HasValue)
            {
                ModelState.AddModelError("ChildBuffet.StartDate", "กรุณากำหนดวันที่เริ่มและสิ้นสุดโปรโมชั่น");
            }
            else if (model.ChildBuffet.EndDate.Value.Date < model.ChildBuffet.StartDate.Value.Date)
            {
                ModelState.AddModelError("ChildBuffet.EndDate", "วันที่สิ้นสุดต้องไม่ก่อนวันที่เริ่ม");
            }
        }

        if (!string.IsNullOrWhiteSpace(code) && !Regex.IsMatch(code, "^[A-Za-z0-9_-]{3,50}$"))
        {
            ModelState.AddModelError("ChildBuffet.Code", "รหัสใช้ได้เฉพาะตัวอักษรภาษาอังกฤษ ตัวเลข ขีดกลาง หรือขีดล่าง (3-50 ตัว)");
        }

        if (!ModelState.IsValid) return BackOfficeView("Promotions", model);

        var membership = _context.HomePromotions.Single(promotion => promotion.SlotKey == "membership");
        var buffet = _context.HomePromotions.Single(promotion => promotion.SlotKey == "buffet");
        UpdatePromotion(membership, model.Membership);
        UpdatePromotion(buffet, model.Buffet);

        var childPromotion = _context.BuffetChildPromotions.Single(promotion => promotion.PromotionId == 1);
        childPromotion.Code = string.IsNullOrWhiteSpace(code) ? null : code.ToUpperInvariant();
        childPromotion.StartDate = model.ChildBuffet.StartDate?.Date;
        childPromotion.EndDate = model.ChildBuffet.EndDate?.Date;
        childPromotion.IsActive = model.ChildBuffet.IsActive;

        _context.SaveChanges();
        TempData["AdminMessage"] = "บันทึกโปรโมชั่นเรียบร้อยแล้ว";
        return RedirectToAction(nameof(Promotions));
    }

    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.Manager + "," + AppRoles.Receptionist + "," + AppRoles.Chef)]
    public IActionResult Orders()
    {
        ViewBag.AllowedOrderStatuses = GetAllowedOrderStatuses();
        ViewBag.CanReviewPayments = User.IsInRole(AppRoles.Admin) ||
                                   User.IsInRole(AppRoles.Manager) ||
                                   User.IsInRole(AppRoles.Receptionist);
        ViewBag.IsChef = User.IsInRole(AppRoles.Chef);
        ViewBag.CanEditOrders = User.IsInRole(AppRoles.Admin) ||
                                User.IsInRole(AppRoles.Manager) ||
                                User.IsInRole(AppRoles.Chef);
        var orders = _context.Orders
            .AsNoTracking()
            .Include(order => order.User)
            .Include(order => order.DiningTable)
            .OrderByDescending(order => order.OrderDate)
            .ToList();
        var orderIds = orders.Select(order => order.OrderID).ToList();
        ViewBag.OrderLines = _context.OrderItems
            .AsNoTracking()
            .Join(
                _context.Menus.AsNoTracking(),
                orderItem => orderItem.MenuID,
                menu => menu.MenuId,
                (orderItem, menu) => new OrderListItemViewModel
                {
                    OrderId = orderItem.OrderID,
                    MenuName = menu.MenuName,
                    Quantity = orderItem.Quantity
                })
            .Where(item => orderIds.Contains(item.OrderId))
            .ToList()
            .GroupBy(item => item.OrderId)
            .ToDictionary(group => group.Key, group => group.ToList());

        return BackOfficeView("Orders", orders);
    }

    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.Manager + "," + AppRoles.Chef)]
    public IActionResult EditOrder(int id)
    {
        var order = _context.Orders.AsNoTracking().SingleOrDefault(item => item.OrderID == id);
        if (order == null) return NotFound();
        if (!CanEditOrder(order))
        {
            TempData["AdminError"] = "แก้ไขได้เฉพาะออเดอร์ที่ยังไม่เริ่มเตรียมและยังไม่ชำระเงิน";
            return RedirectToAction(nameof(Orders));
        }

        var model = new OrderEditViewModel
        {
            OrderId = order.OrderID,
            OrderStatus = order.OrderStatus,
            PaymentStatus = order.PaymentStatus,
            CurrentTotalAmount = order.TotalAmount,
            Items = _context.OrderItems
                .AsNoTracking()
                .Where(item => item.OrderID == order.OrderID)
                .OrderBy(item => item.OrderItemID)
                .Select(item => new OrderEditLineViewModel
                {
                    MenuId = item.MenuID,
                    Quantity = item.Quantity
                })
                .ToList(),
            Menus = GetOrderMenuOptions()
        };

        return BackOfficeView("EditOrder", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.Manager + "," + AppRoles.Chef)]
    public IActionResult EditOrder(OrderEditViewModel model)
    {
        using var transaction = _context.Database.BeginTransaction();
        var order = _context.Orders
            .FromSqlInterpolated($"SELECT * FROM `orders` WHERE `OrderID` = {model.OrderId} FOR UPDATE")
            .AsEnumerable()
            .SingleOrDefault();
        if (order == null) return NotFound();
        if (!CanEditOrder(order))
        {
            TempData["AdminError"] = "แก้ไขได้เฉพาะออเดอร์ที่ยังไม่เริ่มเตรียมและยังไม่ชำระเงิน";
            return RedirectToAction(nameof(Orders));
        }

        var existingItems = _context.OrderItems
            .Where(item => item.OrderID == order.OrderID)
            .ToList();
        var postedItems = model.Items ?? new List<OrderEditLineViewModel>();
        if (postedItems.Count == 0)
        {
            ModelState.AddModelError(nameof(model.Items), "ออเดอร์ต้องมีรายการอาหารอย่างน้อย 1 รายการ");
        }

        var requestedQuantities = postedItems
            .GroupBy(item => item.MenuId)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.Quantity));
        if (requestedQuantities.Values.Any(quantity => quantity > 99))
        {
            ModelState.AddModelError(nameof(model.Items), "จำนวนรวมของแต่ละเมนูต้องไม่เกิน 99");
        }

        if (!ModelState.IsValid)
        {
            return RenderOrderEdit(model, order);
        }

        var originalQuantities = existingItems
            .GroupBy(item => item.MenuID)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.Quantity));
        var originalTrackedQuantities = existingItems
            .Where(item => item.StockDeducted)
            .GroupBy(item => item.MenuID)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.Quantity));
        var menuIds = originalQuantities.Keys
            .Concat(requestedQuantities.Keys)
            .Distinct()
            .OrderBy(menuId => menuId)
            .ToList();
        var menus = new Dictionary<int, Menu>();

        foreach (var menuId in menuIds)
        {
            var menu = _context.Menus
                .FromSqlInterpolated($"SELECT * FROM `menus` WHERE `MenuID` = {menuId} FOR UPDATE")
                .AsEnumerable()
                .SingleOrDefault();
            if (menu != null)
            {
                menus.Add(menu.MenuId, menu);
            }
        }

        if (menus.Count != menuIds.Count)
        {
            ModelState.AddModelError(nameof(model.Items), "พบเมนูที่ไม่มีอยู่ในระบบ กรุณาตรวจสอบรายการอาหาร");
        }
        else
        {
            foreach (var requested in requestedQuantities)
            {
                var menu = menus[requested.Key];
                var originalQuantity = originalQuantities.TryGetValue(requested.Key, out var oldQuantity)
                    ? oldQuantity
                    : 0;
                if (menu.Status != "Available" && requested.Value > originalQuantity)
                {
                    ModelState.AddModelError(nameof(model.Items), $"เมนู {menu.MenuName} ปิดขายอยู่ ไม่สามารถเพิ่มจำนวนได้");
                }

                if (menu.StockQuantity is int stockQuantity)
                {
                    var returnedQuantity = originalTrackedQuantities.TryGetValue(requested.Key, out var trackedQuantity)
                        ? trackedQuantity
                        : 0;
                    if (requested.Value > stockQuantity + returnedQuantity)
                    {
                        ModelState.AddModelError(nameof(model.Items), $"สต๊อกเมนู {menu.MenuName} ไม่เพียงพอ");
                    }
                }
            }
        }

        if (!ModelState.IsValid)
        {
            return RenderOrderEdit(model, order);
        }

        foreach (var menu in menus.Values)
        {
            if (menu.StockQuantity is int stockQuantity)
            {
                var returnedQuantity = originalTrackedQuantities.TryGetValue(menu.MenuId, out var trackedQuantity)
                    ? trackedQuantity
                    : 0;
                var requestedQuantity = requestedQuantities.TryGetValue(menu.MenuId, out var newQuantity)
                    ? newQuantity
                    : 0;
                menu.StockQuantity = stockQuantity + returnedQuantity - requestedQuantity;
            }
        }

        var newTotalAmount = Math.Max(
            0m,
            requestedQuantities.Sum(item => menus[item.Key].Price * item.Value) - order.DiscountAmount);
        if (newTotalAmount != order.TotalAmount &&
            order.PaymentMethod == "PromptPay" &&
            order.PaymentStatus == PaymentStatuses.PendingReview)
        {
            order.PaymentStatus = PaymentStatuses.NeedsResubmission;
        }

        order.TotalAmount = newTotalAmount;
        _context.OrderItems.RemoveRange(existingItems);
        foreach (var requested in requestedQuantities)
        {
            var menu = menus[requested.Key];
            _context.OrderItems.Add(new OrderItem
            {
                OrderID = order.OrderID,
                MenuID = menu.MenuId,
                Quantity = requested.Value,
                Price = menu.Price,
                StockDeducted = menu.StockQuantity.HasValue
            });
        }

        _context.SaveChanges();
        transaction.Commit();

        TempData["AdminMessage"] = $"แก้ไขออเดอร์ #ORD{order.OrderID:D4} เรียบร้อยแล้ว";
        return RedirectToAction(nameof(Orders));
    }

    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.Manager + "," + AppRoles.Receptionist)]
    public IActionResult PaymentProof(int id)
    {
        var order = _context.Orders.AsNoTracking().SingleOrDefault(item => item.OrderID == id);
        if (order == null || order.PaymentMethod != "PromptPay") return NotFound();

        var path = PaymentProofStorage.FindPath(_environment.ContentRootPath, id);
        if (path == null) return NotFound();

        Response.Headers["X-Content-Type-Options"] = "nosniff";
        var contentType = Path.GetExtension(path) == ".png" ? "image/png" : "image/jpeg";
        return PhysicalFile(path, contentType);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.Manager + "," + AppRoles.Receptionist)]
    public IActionResult ConfirmPayment(int id)
    {
        var order = _context.Orders.SingleOrDefault(item => item.OrderID == id);
        if (order == null) return NotFound();
        if (order.OrderStatus == "ยกเลิก" || order.PaymentStatus == PaymentStatuses.Paid)
        {
            TempData["AdminError"] = "ออเดอร์นี้ยกเลิกหรือยืนยันการชำระเงินแล้ว";
            return RedirectToAction(nameof(Orders));
        }

        var isCash = order.PaymentMethod == "เงินสด (ชำระที่หน้าร้าน)";
        var isPromptPay = order.PaymentMethod == "PromptPay";
        if (!isCash && !isPromptPay)
        {
            TempData["AdminError"] = "ไม่รองรับการยืนยันช่องทางชำระเงินนี้";
            return RedirectToAction(nameof(Orders));
        }
        if (isPromptPay &&
            (order.PaymentStatus != PaymentStatuses.PendingReview ||
             PaymentProofStorage.FindPath(_environment.ContentRootPath, id) == null))
        {
            TempData["AdminError"] = "ยังไม่มีสลิปที่รอตรวจสอบสำหรับออเดอร์นี้";
            return RedirectToAction(nameof(Orders));
        }

        order.PaymentStatus = PaymentStatuses.Paid;
        _context.SaveChanges();
        TempData["AdminMessage"] = $"ยืนยันรับชำระเงินออเดอร์ #ORD{id:D4} แล้ว";
        return RedirectToAction(nameof(Orders));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.Manager + "," + AppRoles.Receptionist)]
    public IActionResult RejectPayment(int id)
    {
        var order = _context.Orders.SingleOrDefault(item => item.OrderID == id);
        if (order == null) return NotFound();
        if (order.PaymentMethod != "PromptPay" ||
            order.PaymentStatus != PaymentStatuses.PendingReview)
        {
            TempData["AdminError"] = "ออเดอร์นี้ไม่มีสลิปที่รอตรวจสอบ";
            return RedirectToAction(nameof(Orders));
        }

        order.PaymentStatus = PaymentStatuses.NeedsResubmission;
        _context.SaveChanges();
        TempData["AdminMessage"] = $"ปฏิเสธสลิปออเดอร์ #ORD{id:D4} แล้ว ลูกค้าส่งสลิปใหม่ได้";
        return RedirectToAction(nameof(Orders));
    }

    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.Manager)]
    public IActionResult Reports(DateTime? fromDate, DateTime? toDate)
    {
        var today = DateTime.Today;
        if (!ModelState.IsValid)
        {
            return BackOfficeView("Reports", new SalesReportViewModel
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
            return BackOfficeView("Reports", new SalesReportViewModel
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
                order.PaymentStatus == PaymentStatuses.Paid &&
                order.OrderDate >= startDate &&
                order.OrderDate < endExclusive);
        var completedOrderCount = completedOrders.Count();
        var totalSales = completedOrders
            .Select(order => (decimal?)order.TotalAmount)
            .Sum() ?? 0m;

        return BackOfficeView("Reports", new SalesReportViewModel
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
        if (User.IsInRole(AppRoles.Chef) && !IsAllowedChefStatusTransition(order, status))
        {
            TempData["AdminError"] = "เชฟสามารถเริ่มเตรียมออเดอร์ที่รอดำเนินการ หรือทำเครื่องหมายพร้อมรับหลังเตรียมเสร็จได้เท่านั้น";
            return RedirectToAction(nameof(Orders));
        }
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
        if (status == "ยกเลิก" && order.PaymentStatus == PaymentStatuses.Paid)
        {
            TempData["AdminError"] = "ออเดอร์นี้รับชำระเงินแล้ว กรุณาดำเนินการคืนเงินก่อนยกเลิก";
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
        return BackOfficeView("Stock", _context.Menus
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
        if (User.IsInRole(AppRoles.Admin))
        {
            return RedirectToAction("Index", "Accounts");
        }

        return BackOfficeView("Users", _context.Users
            .AsNoTracking()
            .Where(user => user.Role != AppRoles.Customer)
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

    private IActionResult BackOfficeView(string viewName, object model)
    {
        if (User.IsInRole(AppRoles.Manager))
        {
            return View($"~/Views/Manager/{viewName}.cshtml", model);
        }

        var isStaff = User.IsInRole(AppRoles.Receptionist) ||
                      User.IsInRole(AppRoles.Chef) ||
                      User.IsInRole(AppRoles.StockChecker);
        return isStaff
            ? View($"~/Views/Staff/{viewName}.cshtml", model)
            : View(viewName, model);
    }

    private bool CanEditOrder(Order order) =>
        order.BuffetGuestCount is null &&
        order.PaymentStatus != PaymentStatuses.Paid &&
        (order.OrderStatus == "รอดำเนินการ" || order.OrderStatus == "ยืนยันแล้ว");

    private static bool IsAllowedChefStatusTransition(Order order, string status) =>
        (order.OrderStatus == "รอดำเนินการ" || order.OrderStatus == "ยืนยันแล้ว") &&
            status == "กำลังเตรียม" ||
        order.OrderStatus == "กำลังเตรียม" && status == "พร้อมรับ";

    private IActionResult RenderOrderEdit(OrderEditViewModel model, Order order)
    {
        model.OrderStatus = order.OrderStatus;
        model.PaymentStatus = order.PaymentStatus;
        model.CurrentTotalAmount = order.TotalAmount;
        model.Menus = GetOrderMenuOptions();
        return BackOfficeView("EditOrder", model);
    }

    private List<OrderMenuOptionViewModel> GetOrderMenuOptions() =>
        _context.Menus
            .AsNoTracking()
            .OrderBy(menu => menu.MenuName)
            .Select(menu => new OrderMenuOptionViewModel
            {
                MenuId = menu.MenuId,
                MenuName = menu.MenuName,
                Price = menu.Price,
                Status = menu.Status,
                StockQuantity = menu.StockQuantity
            })
            .ToList();

    private static HomePromotionInputViewModel ToPromotionInput(HomePromotion promotion) =>
        new()
        {
            Kicker = promotion.Kicker,
            Headline = promotion.Headline,
            Description = promotion.Description,
            LinkText = promotion.LinkText,
            IsActive = promotion.IsActive
        };

    private static void UpdatePromotion(HomePromotion promotion, HomePromotionInputViewModel input)
    {
        promotion.Kicker = input.Kicker.Trim();
        promotion.Headline = input.Headline.Trim();
        promotion.Description = input.Description.Trim();
        promotion.LinkText = input.LinkText.Trim();
        promotion.IsActive = input.IsActive;
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
