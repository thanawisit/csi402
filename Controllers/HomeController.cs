using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using IsanDer.Models;

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
    private readonly IsanDerDbContext _context;

    public HomeController(IsanDerDbContext context)
    {
        _context = context;
    }

    public IActionResult Index() => View();
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

        ViewBag.TableNumber = table?.TableNumber;
        return View();
    }
    // ลบ OrderHistory() ตัวเก่าตรงนี้ออกไปแล้วครับ เพื่อไม่ให้ซ้ำกัน

    public IActionResult Menu(int? tableId, string? q, string? category)
    {
        var table = GetActiveTable(tableId);
        if (tableId.HasValue && table == null) return BadRequest();

        ViewBag.TableId = table?.TableID;
        ViewBag.TableNumber = table?.TableNumber;
        ViewBag.OrderError = TempData["OrderError"] as string;
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
        return View(menuList);
    }

    public IActionResult FoodDetail(int id, int? tableId)
    {
        var table = GetActiveTable(tableId);
        if (tableId.HasValue && table == null) return BadRequest();

        var menu = _context.Menus.FirstOrDefault(m => m.MenuId == id);
        if (menu == null) return RedirectToAction("Menu");
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
            return RedirectToAction(nameof(Cart), new { tableId });
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
        return RedirectToAction(nameof(Cart), new { tableId });
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
    public IActionResult SaveOrder(string? paymentMethod, string? arriveTime, int? tableId)
    {
        if (User.Identity == null || !User.Identity.IsAuthenticated) return RedirectToAction("Login", "Account");
        var cart = GetCart();
        if (cart.Count == 0) return RedirectToAction(nameof(Cart), new { tableId });

        var table = GetActiveTable(tableId);
        if (tableId.HasValue && table == null) return BadRequest();

        var isPreorder = table == null;
        if (isPreorder && !IsFuturePickupTime(arriveTime))
        {
            TempData["OrderError"] = "กรุณาเลือกเวลารับอาหารที่ยังมาไม่ถึง";
            return RedirectToAction(nameof(Cart));
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

        decimal totalAmount = cart.Sum(item => menus[item.MenuId].Price * item.Qty);

        var newOrder = new Order
        {
            UserID = user.UserId,
            TableID = table?.TableID,
            OrderDate = System.DateTime.Now,
            OrderType = isPreorder ? PreorderPickupOrderType : DineInOrderType,
            ArriveTime = isPreorder ? arriveTime! : "-",
            PaymentMethod = paymentMethod ?? "เงินสด (ชำระที่หน้าร้าน)",
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
        transaction.Commit();
        ClearCart();

        return RedirectToAction("OrderHistory");  
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