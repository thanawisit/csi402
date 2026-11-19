using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using IsanDer.Models;
using IsanDer.Services;

namespace IsanDer.Controllers;

public class CartItem
{
    public int MenuId { get; set; }
    public string MenuName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Qty { get; set; }
}

public class HomeController : Controller
{
    private const string DineInOrderType = "ทานที่ร้าน";
    private const string PreorderPickupOrderType = "สั่งล่วงหน้า รับที่ร้าน";
    private const string BuffetOrderType = "บุฟเฟต์";
    private const decimal BuffetPricePerPerson = 299m;
    private const string FreePromotionPaymentMethod = "โปรโมชั่น (ไม่มียอดชำระ)";
    private readonly IsanDerDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public HomeController(IsanDerDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    public IActionResult Index()
    {
        var featuredMenus = _context.Menus
            .AsNoTracking()
            .Where(menu => menu.Status == "Available" && (menu.StockQuantity == null || menu.StockQuantity > 0))
            .OrderBy(menu => menu.MenuId)
            .Take(3)
            .ToList();
        foreach (var menu in featuredMenus)
        {
            menu.ImagePath = MenuImageStorage.GetPublicPath(_environment.WebRootPath, menu.MenuId);
        }

        return View(new HomePageViewModel
        {
            Promotions = _context.HomePromotions
                .AsNoTracking()
                .Where(promotion => promotion.IsActive)
                .OrderBy(promotion => promotion.SortOrder)
                .ToList(),
            FeaturedMenus = featuredMenus
        });
    }
    public IActionResult Privacy() => View();
    public IActionResult Terms() => View();
    public IActionResult Contact() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    public IActionResult Buffet() => View();

    public IActionResult ReserveTable(string? package)
    {
        if (!IsValidPackage(package)) return BadRequest();

        ViewBag.Package = package;
        var tables = _context.DiningTables
            .Where(table => table.IsActive)
            .OrderByDescending(table => table.Capacity)
            .ThenBy(table => table.TableNumber)
            .ToList();
        return View(tables);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ConfirmTable(string? package, int? tableId)
    {
        if (!IsValidPackage(package)) return BadRequest();

        var table = GetActiveTable(tableId);
        if (table == null) return BadRequest();

        if (package == "alacarte")
        {
            return RedirectToAction(nameof(Menu), new { tableId = table.TableID });
        }

        return RedirectToAction(nameof(ScanQR), new { tableId = table.TableID });
    }

    public IActionResult ScanQR(int? tableId)
    {
        var table = GetActiveTable(tableId);
        if (tableId.HasValue && table == null) return BadRequest();

        ViewBag.TableId = table?.TableID;
        ViewBag.TableNumber = table?.TableNumber;
        ViewBag.TableCapacity = table?.Capacity;
        return View();
    }

    [Authorize(Roles = AppRoles.Customer)]
    [HttpGet]
    public IActionResult BuffetCheckout(
        int tableId,
        int guestCount,
        string? promoCode,
        int buffetFreeChildCount = 0)
    {
        var table = GetActiveTable(tableId);
        if (table == null) return BadRequest();
        if (guestCount < 1 || guestCount > table.Capacity) return BadRequest();

        var discountAmount = 0m;
        if (!TryCalculateBuffetChildDiscount(
                guestCount,
                promoCode,
                buffetFreeChildCount,
                out discountAmount,
                out var promoError))
        {
            ViewBag.PromoError = promoError;
            discountAmount = 0m;
        }

        ViewBag.DiscountAmount = discountAmount;
        ViewBag.TotalAmount = BuffetPricePerPerson * guestCount - discountAmount;
        ViewBag.OrderType = BuffetOrderType;
        ViewBag.ArriveTime = "-";
        ViewBag.IsPreorder = false;
        ViewBag.IsBuffet = true;
        ViewBag.BuffetGuestCount = guestCount;
        ViewBag.BuffetFreeChildCount = buffetFreeChildCount;
        ViewBag.PromoCode = promoCode?.Trim();
        ViewBag.BuffetPricePerPerson = BuffetPricePerPerson;
        ViewBag.OrderError = TempData["OrderError"] as string;
        ViewBag.TableId = table.TableID;
        ViewBag.TableNumber = table.TableNumber;
        return View("Payment");
    }
    // ลบ OrderHistory() ตัวเก่าตรงนี้ออกไปแล้วครับ เพื่อไม่ให้ซ้ำกัน

    public IActionResult Menu(int? tableId, string? q, string? category)
    {
        var table = GetActiveTable(tableId);
        if (tableId.HasValue && table == null) return BadRequest();

        ViewBag.TableId = table?.TableID;
        ViewBag.TableNumber = table?.TableNumber;
        ViewBag.OrderError = TempData["OrderError"] as string;
        ViewBag.OrderSuccess = TempData["OrderSuccess"] as string;
        var searchTerm = q?.Trim();
        var selectedCategory = category?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(selectedCategory) || selectedCategory == "all")
        {
            selectedCategory = null;
        }

        ViewBag.SearchTerm = searchTerm;
        ViewBag.MenuCategory = selectedCategory;

        var menuQuery = _context.Menus.AsNoTracking();
        switch (selectedCategory)
        {
            case null:
                break;
            case "somtum":
                menuQuery = menuQuery.Where(menu => menu.MenuName.Contains("ส้มตำ"));
                break;
            case "larb":
                menuQuery = menuQuery.Where(menu =>
                    menu.MenuName.Contains("ลาบ") || menu.MenuName.Contains("น้ำตก"));
                break;
            case "soup":
                menuQuery = menuQuery.Where(menu =>
                    menu.MenuName.Contains("ต้ม") || menu.MenuName.Contains("แกง"));
                break;
            case "grilled":
                menuQuery = menuQuery.Where(menu => menu.MenuName.Contains("ย่าง"));
                break;
            case "drinks":
                menuQuery = menuQuery.Where(menu =>
                    menu.MenuName.Contains("เครื่องดื่ม") ||
                    menu.MenuName.Contains("ชา") ||
                    menu.MenuName.Contains("กาแฟ") ||
                    menu.MenuName.Contains("โค้ก") ||
                    menu.MenuName.Contains("น้ำเปล่า") ||
                    menu.MenuName.Contains("น้ำอัดลม") ||
                    menu.MenuName.Contains("น้ำผลไม้"));
                break;
            default:
                return BadRequest();
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            menuQuery = menuQuery.Where(menu => menu.MenuName.Contains(searchTerm));
        }

        var menuList = menuQuery.ToList();
        foreach (var menu in menuList)
        {
            menu.ImagePath = MenuImageStorage.GetPublicPath(_environment.WebRootPath, menu.MenuId);
        }

        return View(menuList);
    }

    public IActionResult FoodDetail(int id, int? tableId)
    {
        var table = GetActiveTable(tableId);
        if (tableId.HasValue && table == null) return BadRequest();

        var menu = _context.Menus.FirstOrDefault(m => m.MenuId == id);
        if (menu == null) return RedirectToAction("Menu");
        menu.ImagePath = MenuImageStorage.GetPublicPath(_environment.WebRootPath, menu.MenuId);
        ViewBag.TableId = table?.TableID;
        ViewBag.TableNumber = table?.TableNumber;
        return View(menu);
    }

    // --- ฟังก์ชันเกี่ยวกับการสั่งอาหาร (ต้อง Login ก่อน) ---

    [Authorize(Roles = AppRoles.Customer)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddToCart(int menuId, int qty, int? tableId)
    {
        if (User.Identity == null || !User.Identity.IsAuthenticated)
        {
            return RedirectToAction("Login", "Account");
        }
        if (qty < 1) return BadRequest();
        if (tableId.HasValue && GetActiveTable(tableId) == null) return BadRequest();

        var menu = _context.Menus.FirstOrDefault(m => m.MenuId == menuId);
        if (menu == null) return NotFound();
        if (!IsMenuAvailable(menu) || menu.StockQuantity == 0)
        {
            TempData["OrderError"] = "เมนูนี้ปิดขายหรือสินค้าหมดแล้ว";
            return RedirectToAction(nameof(Menu), new { tableId });
        }

        var cart = GetCart();
        var existingItem = cart.FirstOrDefault(c => c.MenuId == menuId);
        var requestedQuantity = (existingItem?.Qty ?? 0) + qty;
        if (menu.StockQuantity.HasValue && requestedQuantity > menu.StockQuantity.Value)
        {
            TempData["OrderError"] = "จำนวนสินค้าในสต๊อกไม่เพียงพอ";
            return RedirectToAction(nameof(Menu), new { tableId });
        }

        if (existingItem != null)
        {
            existingItem.Qty += qty;
        }
        else
        {
            cart.Add(new CartItem { MenuId = menu.MenuId, MenuName = menu.MenuName, Price = menu.Price, Qty = qty });
        }
        SaveCart(cart);
        TempData["OrderSuccess"] = $"เพิ่ม {menu.MenuName} ลงตะกร้าแล้ว";
        return RedirectToAction(nameof(Menu), new { tableId });
    }

    [Authorize(Roles = AppRoles.Customer)]
    [HttpPost]
    public IActionResult IncreaseQty(int menuId, int? tableId)
    {
        if (User.Identity == null || !User.Identity.IsAuthenticated) return RedirectToAction("Login", "Account");
        if (tableId.HasValue && GetActiveTable(tableId) == null) return BadRequest();

        var cart = GetCart();
        var item = cart.FirstOrDefault(c => c.MenuId == menuId);
        if (item != null)
        {
            var menu = _context.Menus.SingleOrDefault(existingMenu => existingMenu.MenuId == menuId);
            if (menu == null || !IsMenuAvailable(menu) ||
                (menu.StockQuantity.HasValue && item.Qty >= menu.StockQuantity.Value))
            {
                TempData["OrderError"] = "จำนวนสินค้าในสต๊อกไม่เพียงพอ";
            }
            else
            {
                item.Qty++;
            }
        }
        SaveCart(cart);
        return RedirectToAction(nameof(Cart), new { tableId });
    }

    [Authorize(Roles = AppRoles.Customer)]
    [HttpPost]
    public IActionResult DecreaseQty(int menuId, int? tableId)
    {
        if (User.Identity == null || !User.Identity.IsAuthenticated) return RedirectToAction("Login", "Account");
        if (tableId.HasValue && GetActiveTable(tableId) == null) return BadRequest();

        var cart = GetCart();
        var item = cart.FirstOrDefault(c => c.MenuId == menuId);
        if (item != null)
        {
            if (item.Qty > 1) item.Qty--;
            else cart.Remove(item);
        }
        SaveCart(cart);
        return RedirectToAction(nameof(Cart), new { tableId });
    }

    [Authorize(Roles = AppRoles.Customer)]
    public IActionResult Cart(int? tableId)
    {
        if (User.Identity == null || !User.Identity.IsAuthenticated)
        {
            return RedirectToAction("Login", "Account");
        }
        var table = GetActiveTable(tableId);
        if (tableId.HasValue && table == null) return BadRequest();

        ViewBag.TableId = table?.TableID;
        ViewBag.TableNumber = table?.TableNumber;
        ViewBag.IsPreorder = table == null;
        ViewBag.MinPickupTime = DateTime.Now.AddMinutes(1).ToString("HH:mm", CultureInfo.InvariantCulture);
        ViewBag.OrderError = TempData["OrderError"] as string;
        return View(GetCart());
    }

    [Authorize(Roles = AppRoles.Customer)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Payment(string? arriveTime, int? tableId)
    {
        if (User.Identity == null || !User.Identity.IsAuthenticated)
        {
            return RedirectToAction("Login", "Account");
        }
        if (tableId.HasValue && GetActiveTable(tableId) == null) return BadRequest();

        var cart = GetCart();
        if (cart.Count == 0) return RedirectToAction(nameof(Cart), new { tableId });

        var table = GetActiveTable(tableId);
        var isPreorder = table == null;
        if (isPreorder && !IsFuturePickupTime(arriveTime))
        {
            TempData["OrderError"] = "กรุณาเลือกเวลารับอาหารที่ยังมาไม่ถึง";
            return RedirectToAction(nameof(Cart));
        }

        decimal totalAmount = cart.Sum(item => item.Price * item.Qty);
        ViewBag.TotalAmount = totalAmount; 
        ViewBag.OrderType = isPreorder ? PreorderPickupOrderType : DineInOrderType;
        ViewBag.ArriveTime = isPreorder ? arriveTime : "-";
        ViewBag.IsPreorder = isPreorder;
        ViewBag.IsBuffet = false;
        ViewBag.BuffetGuestCount = null;
        ViewBag.TableId = tableId;
        ViewBag.TableNumber = table?.TableNumber;
        
        return View();
    }

    [Authorize(Roles = AppRoles.Customer)]
    public IActionResult Profile()
    {
        if (User.Identity == null || !User.Identity.IsAuthenticated)
        {
            return RedirectToAction("Login", "Account");
        }

        var userEmail = User.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.Email)?.Value;
        if (string.IsNullOrWhiteSpace(userEmail))
        {
            return Challenge();
        }

        var userProfile = _context.Users.SingleOrDefault(user => user.Email == userEmail);
        if (userProfile == null) return NotFound();

        return View(userProfile);
    }

    // เพิ่มฟังก์ชันนี้สำหรับบันทึกข้อมูลลงฐานข้อมูลตอนกดชำระเงิน
    [Authorize(Roles = AppRoles.Customer)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> SaveOrder(
        string? paymentMethod,
        string? arriveTime,
        int? tableId,
        int? buffetGuestCount,
        string? promoCode,
        int? buffetFreeChildCount,
        IFormFile? paymentProof)
    {
        if (User.Identity == null || !User.Identity.IsAuthenticated) return RedirectToAction("Login", "Account");
        const string cashMethod = "เงินสด (ชำระที่หน้าร้าน)";
        const string promptPayMethod = "PromptPay";

        var isBuffet = buffetGuestCount.HasValue;
        var guestCount = buffetGuestCount.GetValueOrDefault();
        var freeChildCount = buffetFreeChildCount.GetValueOrDefault();
        var normalizedPromoCode = string.IsNullOrWhiteSpace(promoCode) ? null : promoCode.Trim();
        var table = GetActiveTable(tableId);
        if (tableId.HasValue && table == null) return BadRequest();
        if (isBuffet && (table == null || guestCount < 1 || guestCount > table.Capacity))
        {
            return BadRequest();
        }
        if (paymentMethod != cashMethod &&
            paymentMethod != promptPayMethod &&
            !(isBuffet && paymentMethod == FreePromotionPaymentMethod))
        {
            return BadRequest();
        }

        var discountAmount = 0m;
        if (isBuffet)
        {
            if (!TryCalculateBuffetChildDiscount(
                    guestCount,
                    normalizedPromoCode,
                    freeChildCount,
                    out discountAmount,
                    out var promoError))
            {
                TempData["OrderError"] = promoError;
                return RedirectToAction(nameof(BuffetCheckout), new
                {
                    tableId,
                    guestCount,
                    promoCode = normalizedPromoCode,
                    buffetFreeChildCount = freeChildCount
                });
            }
        }
        else if (normalizedPromoCode != null || freeChildCount != 0)
        {
            return BadRequest();
        }

        var cart = isBuffet ? new List<CartItem>() : GetCart();
        if (!isBuffet && cart.Count == 0) return RedirectToAction(nameof(Cart), new { tableId });

        var isPreorder = table == null;
        if (isPreorder && !IsFuturePickupTime(arriveTime))
        {
            TempData["OrderError"] = "กรุณาเลือกเวลารับอาหารที่ยังมาไม่ถึง";
            return RedirectToAction(nameof(Cart), new { tableId });
        }

        var userEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        var user = _context.Users.FirstOrDefault(u => u.Email == userEmail);
        if (user == null) return RedirectToAction("Login", "Account");

        using var transaction = _context.Database.BeginTransaction();
        var menuIds = cart.Select(item => item.MenuId).Distinct().ToList();
        var menus = new Dictionary<int, Menu>();
        foreach (var menuId in menuIds.OrderBy(id => id))
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

        if (menus.Count != menuIds.Count ||
            cart.Any(item => !menus.TryGetValue(item.MenuId, out var menu) ||
                             !IsMenuAvailable(menu) ||
                             (menu.StockQuantity.HasValue && menu.StockQuantity.Value < item.Qty)))
        {
            TempData["OrderError"] = "มีเมนูปิดขายหรือสต๊อกไม่เพียงพอ กรุณาตรวจสอบตะกร้าอีกครั้ง";
            return RedirectToAction(nameof(Cart), new { tableId });
        }

        decimal totalAmount = isBuffet
            ? BuffetPricePerPerson * guestCount - discountAmount
            : cart.Sum(item => menus[item.MenuId].Price * item.Qty);

        if (isBuffet && totalAmount == 0m)
        {
            paymentMethod = FreePromotionPaymentMethod;
        }
        else if (paymentMethod == FreePromotionPaymentMethod)
        {
            return BadRequest();
        }

        var isPromptPay = paymentMethod == promptPayMethod;
        var proofExtension = string.Empty;
        if (isPromptPay && !PaymentProofStorage.TryGetExtension(paymentProof, out proofExtension))
        {
            TempData["OrderError"] = paymentProof?.Length > PaymentProofStorage.MaxFileSize
                ? "ไฟล์สลิปต้องมีขนาดไม่เกิน 5 MB"
                : "กรุณาแนบสลิปเป็นไฟล์ PNG หรือ JPEG";
            if (isBuffet)
            {
                return RedirectToAction(nameof(BuffetCheckout), new
                {
                    tableId,
                    guestCount,
                    promoCode = normalizedPromoCode,
                    buffetFreeChildCount = freeChildCount
                });
            }
            return RedirectToAction(nameof(Cart), new { tableId });
        }
        if (!isPromptPay && paymentProof is { Length: > 0 }) return BadRequest();

        var newOrder = new Order
        {
            UserID = user.UserId,
            TableID = table?.TableID,
            OrderDate = System.DateTime.Now,
            OrderType = isBuffet ? BuffetOrderType : isPreorder ? PreorderPickupOrderType : DineInOrderType,
            ArriveTime = isPreorder ? arriveTime! : "-",
            PaymentMethod = paymentMethod!,
            PaymentStatus = isPromptPay
                ? PaymentStatuses.PendingReview
                : totalAmount == 0m
                    ? PaymentStatuses.Paid
                    : PaymentStatuses.Unpaid,
            BuffetGuestCount = isBuffet ? guestCount : null,
            BuffetFreeChildCount = isBuffet ? freeChildCount : 0,
            PromoCode = isBuffet && freeChildCount > 0 ? normalizedPromoCode : null,
            DiscountAmount = discountAmount,
            TotalAmount = totalAmount,
            OrderStatus = "รอดำเนินการ"
        };

        _context.Orders.Add(newOrder);
        _context.SaveChanges();

        foreach (var item in cart)
        {
            var menu = menus[item.MenuId];
            var stockDeducted = menu.StockQuantity.HasValue;
            if (menu.StockQuantity is int stockQuantity)
            {
                menu.StockQuantity = stockQuantity - item.Qty;
            }

            var orderItem = new OrderItem
            {
                OrderID = newOrder.OrderID,
                MenuID = item.MenuId,
                Quantity = item.Qty,
                Price = menu.Price,
                StockDeducted = stockDeducted
            };
            _context.OrderItems.Add(orderItem);
        }
        _context.SaveChanges();

        if (isPromptPay)
        {
            await PaymentProofStorage.SaveAsync(
                _environment.ContentRootPath,
                newOrder.OrderID,
                paymentProof!,
                proofExtension,
                HttpContext.RequestAborted);
        }
        transaction.Commit();
        if (!isBuffet)
        {
            ClearCart();
        }

        return RedirectToAction("OrderHistory");  
    }

    [Authorize(Roles = AppRoles.Customer)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> ResubmitPaymentProof(int id, IFormFile? paymentProof)
    {
        var userEmail = User.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(userEmail)) return Challenge();

        var user = _context.Users.SingleOrDefault(account => account.Email == userEmail);
        if (user == null) return Challenge();

        var order = _context.Orders.SingleOrDefault(item =>
            item.OrderID == id &&
            item.UserID == user.UserId &&
            item.PaymentMethod == "PromptPay");
        if (order == null) return NotFound();
        if (order.PaymentStatus != PaymentStatuses.NeedsResubmission ||
            order.OrderStatus == "ยกเลิก")
        {
            return BadRequest();
        }

        if (!PaymentProofStorage.TryGetExtension(paymentProof, out var extension))
        {
            TempData["PaymentError"] = paymentProof?.Length > PaymentProofStorage.MaxFileSize
                ? "ไฟล์สลิปต้องมีขนาดไม่เกิน 5 MB"
                : "กรุณาแนบสลิปเป็นไฟล์ PNG หรือ JPEG";
            return RedirectToAction(nameof(OrderDetails), new { id });
        }

        await PaymentProofStorage.SaveAsync(
            _environment.ContentRootPath,
            order.OrderID,
            paymentProof!,
            extension,
            HttpContext.RequestAborted);
        order.PaymentStatus = PaymentStatuses.PendingReview;
        _context.SaveChanges();

        TempData["PaymentMessage"] = "ส่งสลิปใหม่แล้ว กรุณารอแอดมินตรวจสอบ";
        return RedirectToAction(nameof(OrderDetails), new { id });
    }

    [Authorize(Roles = AppRoles.Customer)]
    [HttpGet]
    public IActionResult OrderHistory()
    {
        // 1. ตรวจสอบว่าเข้าสู่ระบบหรือยัง
        if (User.Identity == null || !User.Identity.IsAuthenticated) 
            return RedirectToAction("Login", "Account");

        // 2. หาข้อมูลลูกค้าที่กำลังใช้งาน
        var userEmail = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email)?.Value;
        var user = _context.Users.FirstOrDefault(u => u.Email == userEmail);
        
        if (user == null) 
            return RedirectToAction("Login", "Account");

        // 3. ดึงข้อมูลบิลทั้งหมดของลูกค้ารายนี้ เรียงจากบิลใหม่ล่าสุดไปเก่า
        var orders = _context.Orders
                             .Include(o => o.DiningTable)
                             .Where(o => o.UserID == user.UserId)
                             .OrderByDescending(o => o.OrderDate)
                             .ToList();

        // 4. ส่งข้อมูลไปแสดงที่หน้าเว็บ
        return View(orders);
    }

    [Authorize(Roles = AppRoles.Customer)]
    [HttpGet]
    public IActionResult OrderDetails(int id)
    {
        if (User.Identity == null || !User.Identity.IsAuthenticated)
        {
            return RedirectToAction("Login", "Account");
        }

        var userEmail = User.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.Email)?.Value;
        if (string.IsNullOrWhiteSpace(userEmail))
        {
            return Challenge();
        }

        var user = _context.Users.SingleOrDefault(account => account.Email == userEmail);
        if (user == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var order = _context.Orders
            .AsNoTracking()
            .Include(existingOrder => existingOrder.DiningTable)
            .SingleOrDefault(existingOrder =>
                existingOrder.OrderID == id && existingOrder.UserID == user.UserId);
        if (order == null)
        {
            return NotFound();
        }

        var items = (from orderItem in _context.OrderItems.AsNoTracking()
                     join menu in _context.Menus.AsNoTracking()
                         on orderItem.MenuID equals menu.MenuId
                     where orderItem.OrderID == order.OrderID
                     select new OrderDetailItemViewModel
                     {
                         MenuName = menu.MenuName,
                         Quantity = orderItem.Quantity,
                         UnitPrice = orderItem.Price
                     }).ToList();

        return View(new OrderDetailViewModel
        {
            Order = order,
            Items = items
        });
    }

    private bool TryCalculateBuffetChildDiscount(
        int guestCount,
        string? promoCode,
        int freeChildCount,
        out decimal discountAmount,
        out string? errorMessage)
    {
        discountAmount = 0m;
        errorMessage = null;
        var normalizedCode = promoCode?.Trim();
        if (string.IsNullOrEmpty(normalizedCode))
        {
            if (freeChildCount == 0) return true;

            errorMessage = "กรุณากรอกรหัสโปรโมชั่นสำหรับเด็กก่อน";
            return false;
        }

        if (freeChildCount < 1 || freeChildCount > guestCount)
        {
            errorMessage = "จำนวนเด็กที่ใช้สิทธิ์ต้องอยู่ระหว่าง 1 คนถึงจำนวนผู้ใช้บุฟเฟต์";
            return false;
        }

        var promotion = _context.BuffetChildPromotions
            .AsNoTracking()
            .Single(item => item.PromotionId == 1);
        if (!promotion.IsActive ||
            !string.Equals(promotion.Code, normalizedCode, StringComparison.OrdinalIgnoreCase))
        {
            errorMessage = "รหัสโปรโมชั่นไม่ถูกต้องหรือไม่ได้เปิดใช้งาน";
            return false;
        }

        var today = DateTime.Today;
        if (!promotion.StartDate.HasValue ||
            !promotion.EndDate.HasValue ||
            today < promotion.StartDate.Value.Date ||
            today > promotion.EndDate.Value.Date)
        {
            errorMessage = "รหัสโปรโมชั่นนี้อยู่นอกช่วงวันที่ใช้งาน";
            return false;
        }

        discountAmount = BuffetPricePerPerson * freeChildCount;
        return true;
    }

    private static bool IsValidPackage(string? package) =>
        package is null or "buffet" or "alacarte";

    private static bool IsFuturePickupTime(string? arriveTime) =>
        TimeOnly.TryParseExact(arriveTime, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var pickupTime) &&
        pickupTime > TimeOnly.FromDateTime(DateTime.Now);

    private static bool IsMenuAvailable(Menu menu) =>
        string.Equals(menu.Status, "Available", StringComparison.OrdinalIgnoreCase);

    private DiningTable? GetActiveTable(int? tableId) =>
        tableId.HasValue
            ? _context.DiningTables.SingleOrDefault(table => table.TableID == tableId.Value && table.IsActive)
            : null;

    private string CartSessionKey
    {
        get
        {
            var email = User.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.Email)?.Value;
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new InvalidOperationException("The authenticated user has no email claim.");
            }

            return $"Cart:{email}";
        }
    }

    private List<CartItem> GetCart()
    {
        var cartJson = HttpContext.Session.GetString(CartSessionKey);
        return cartJson == null
            ? new List<CartItem>()
            : JsonSerializer.Deserialize<List<CartItem>>(cartJson) ?? new List<CartItem>();
    }

    private void SaveCart(List<CartItem> cart) =>
        HttpContext.Session.SetString(CartSessionKey, JsonSerializer.Serialize(cart));

    private void ClearCart() =>
        HttpContext.Session.Remove(CartSessionKey);
}