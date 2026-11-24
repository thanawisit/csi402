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
    public string? ImagePath { get; set; }
    public string? SpecialRequest { get; set; }
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
        var currentBuffetPromotion = GetActiveBuffetChildPromotion();
        var today = DateTime.Today;
        var generalPromotions = _context.GeneralPromotions
            .AsNoTracking()
            .Where(promotion =>
                promotion.IsActive &&
                (!promotion.StartDate.HasValue || promotion.StartDate.Value <= today) &&
                (!promotion.EndDate.HasValue || promotion.EndDate.Value >= today))
            .OrderBy(promotion => promotion.PromotionId)
            .Take(12)
            .ToList();
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

        var featuredMenuIds = featuredMenus.Select(menu => menu.MenuId).ToList();
        var featuredOrderItems = _context.OrderItems
            .AsNoTracking()
            .Where(item => featuredMenuIds.Contains(item.MenuID))
            .AsEnumerable()
            .ToList();
        var featuredOrderIdsByMenu = featuredOrderItems
            .GroupBy(item => item.MenuID)
            .ToDictionary(
                group => group.Key,
                group => group.Select(item => item.OrderID).Distinct().ToList());
        var featuredReviews = _context.OrderReviews
            .AsNoTracking()
            .Where(review => !review.IsHidden)
            .ToList();
        var featuredReviewStats = new Dictionary<int, (double Average, int Count)>();
        foreach (var entry in featuredOrderIdsByMenu)
        {
            var ratings = featuredReviews
                .Where(review => entry.Value.Contains(review.OrderId))
                .Select(review => review.Rating)
                .ToList();
            if (ratings.Count > 0)
            {
                featuredReviewStats[entry.Key] =
                    (ratings.Average(), ratings.Count);
            }
        }
        ViewBag.FeaturedReviewStats = featuredReviewStats;

        return View(new HomePageViewModel
        {
            Promotions = _context.HomePromotions
                .AsNoTracking()
                .Where(promotion => promotion.IsActive &&
                    (promotion.SlotKey != "buffet" || currentBuffetPromotion != null))
                .OrderBy(promotion => promotion.SortOrder)
                .ToList(),
            GeneralPromotions = generalPromotions,
            FeaturedMenus = featuredMenus,
            CurrentBuffetPromotion = currentBuffetPromotion
        });
    }

    [HttpGet]
    [Route("promotions")]
    public IActionResult Promotions()
    {
        var today = DateTime.Today;
        var currentBuffetPromotion = GetActiveBuffetChildPromotion();
        var generalPromotions = _context.GeneralPromotions
            .AsNoTracking()
            .Where(promotion =>
                promotion.IsActive &&
                (!promotion.StartDate.HasValue || promotion.StartDate.Value <= today) &&
                (!promotion.EndDate.HasValue || promotion.EndDate.Value >= today))
            .OrderBy(promotion => promotion.PromotionId)
            .ToList();
        var homePromotions = _context.HomePromotions
            .AsNoTracking()
            .Where(promotion =>
                promotion.IsActive &&
                (promotion.SlotKey != "buffet" || currentBuffetPromotion != null))
            .OrderBy(promotion => promotion.SortOrder)
            .ToList();

        ViewData["Title"] = "โปรโมชั่น";
        return View(new HomePageViewModel
        {
            Promotions = homePromotions,
            GeneralPromotions = generalPromotions,
            CurrentBuffetPromotion = currentBuffetPromotion
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

    public IActionResult Buffet()
    {
        ViewBag.CurrentBuffetPromotion = GetActiveBuffetChildPromotion();
        return View();
    }

    public IActionResult ReserveTable(string? package, DateTime? date, string? time)
    {
        if (!IsValidPackage(package)) return BadRequest();

        ViewBag.Package = package;
        ViewBag.RequestedReservationDate = date?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        ViewBag.RequestedReservationTime = string.IsNullOrWhiteSpace(time) ? null : time;
        var tables = _context.DiningTables
            .Where(table => table.IsActive)
            .OrderByDescending(table => table.Capacity)
            .ThenBy(table => table.TableNumber)
            .ToList();

        var requestedStart = GetReservationStart(date, time);
        var requestedEnd = requestedStart?.AddHours(2);
        HashSet<int> occupiedTableIds;

        if (requestedStart.HasValue && requestedEnd.HasValue)
        {
            occupiedTableIds = _context.TableReservations
                .AsNoTracking()
                .Where(reservation =>
                    reservation.Status != ReservationStatuses.Cancelled &&
                    reservation.Status != ReservationStatuses.Completed &&
                    reservation.ReservationDateTime < requestedEnd.Value &&
                    reservation.ReservationEndTime > requestedStart.Value)
                .Select(reservation => reservation.TableID)
                .Distinct()
                .ToHashSet();
        }
        else
        {
            occupiedTableIds = _context.TableReservations
                .AsNoTracking()
                .Where(reservation =>
                    reservation.Status != ReservationStatuses.Cancelled &&
                    reservation.Status != ReservationStatuses.Completed)
                .Select(reservation => reservation.TableID)
                .Distinct()
                .ToHashSet();
        }

        ViewBag.OccupiedTableIds = occupiedTableIds;
        ViewBag.HasRequestedReservationTime = requestedStart.HasValue;
        return View(tables);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ConfirmTable(
        string? package,
        int? tableId,
        DateTime? reservationDate,
        string? reservationTime)
    {
        if (!IsValidPackage(package)) return BadRequest();
        var table = GetActiveTable(tableId);
        if (table == null) return BadRequest();

        var requestedStart = GetReservationStart(reservationDate, reservationTime);
        var requestedEnd = requestedStart?.AddHours(2);
        var isOccupied = requestedStart.HasValue && requestedEnd.HasValue
            ? _context.TableReservations.Any(reservation =>
                reservation.TableID == table.TableID &&
                reservation.Status != ReservationStatuses.Cancelled &&
                reservation.Status != ReservationStatuses.Completed &&
                reservation.ReservationDateTime < requestedEnd.Value &&
                reservation.ReservationEndTime > requestedStart.Value)
            : _context.TableReservations.Any(reservation =>
                reservation.TableID == table.TableID &&
                reservation.Status != ReservationStatuses.Cancelled &&
                reservation.Status != ReservationStatuses.Completed);
        if (isOccupied)
        {
            TempData["TableError"] = "โต๊ะนี้กำลังมีการจองอยู่ กรุณาเลือกโต๊ะว่าง";
            return RedirectToAction(nameof(ReserveTable), new { package });
        }

        if (User.Identity?.IsAuthenticated != true)
        {
            return RedirectToAction("Login", "Account", new
            {
                returnUrl = Url.Action(nameof(ReservationDetails), new { package, tableId = table.TableID })
            });
        }
        if (!User.IsInRole(AppRoles.Customer)) return Forbid();

        return RedirectToAction(nameof(ReservationDetails), new { package, tableId = table.TableID });
    }

    [Authorize(Roles = AppRoles.Customer)]
    public IActionResult ReservationDetails(string? package, int? tableId)
    {
        if (!IsValidPackage(package) || !tableId.HasValue) return BadRequest();

        var table = GetActiveTable(tableId);
        if (table == null) return BadRequest();
        var user = GetCurrentCustomer();
        if (user == null) return Challenge();

        var defaultReservationTime = DateTime.Now.AddHours(1);
        ViewBag.MinReservationDate = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return View(new ReservationDetailsViewModel
        {
            Package = package!,
            TableId = table.TableID,
            TableNumber = table.TableNumber,
            TableCapacity = table.Capacity,
            ReservationDate = defaultReservationTime.Date,
            ReservationTime = TimeOnly.FromDateTime(defaultReservationTime),
            GuestCount = 1,
            CustomerName = user.Username,
            CustomerEmail = user.Email,
            CustomerPhone = user.PhoneNumber ?? string.Empty
        });
    }

    [Authorize(Roles = AppRoles.Customer)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ReservationDetails(ReservationDetailsViewModel model)
    {
        if (!IsValidPackage(model.Package)) return BadRequest();

        var table = GetActiveTable(model.TableId);
        if (table == null) return BadRequest();
        var user = GetCurrentCustomer();
        if (user == null) return Challenge();

        model.TableNumber = table.TableNumber;
        model.TableCapacity = table.Capacity;
        model.CustomerEmail = user.Email;
        ViewBag.MinReservationDate = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        if (model.GuestCount > table.Capacity)
        {
            ModelState.AddModelError(nameof(model.GuestCount), $"โต๊ะนี้รองรับได้ไม่เกิน {table.Capacity} คน");
        }
        if (!model.ReservationDate.HasValue)
        {
            ModelState.AddModelError(nameof(model.ReservationDate), "กรุณาเลือกวันที่ต้องการจอง");
        }
        if (!model.ReservationTime.HasValue)
        {
            ModelState.AddModelError(nameof(model.ReservationTime), "กรุณาเลือกเวลาที่ต้องการจอง");
        }
        if (model.ReservationDate.HasValue && model.ReservationTime.HasValue &&
            model.ReservationDate.Value.Date.Add(model.ReservationTime.Value.ToTimeSpan()) <= DateTime.Now)
        {
            ModelState.AddModelError(nameof(model.ReservationTime), "กรุณาเลือกเวลาในอนาคต");
        }
        if (!ModelState.IsValid) return View(model);

        var reservationStart = model.ReservationDate!.Value.Date
            .Add(model.ReservationTime!.Value.ToTimeSpan());
        var reservationEnd = reservationStart.AddHours(2);
        using var transaction = _context.Database.BeginTransaction();
        var lockedTable = _context.DiningTables
            .FromSqlInterpolated($"SELECT * FROM `dining_tables` WHERE `TableID` = {table.TableID} FOR UPDATE")
            .AsNoTracking()
            .AsEnumerable()
            .SingleOrDefault();
        if (lockedTable?.IsActive != true) return BadRequest();
        if (model.GuestCount > lockedTable.Capacity)
        {
            ModelState.AddModelError(nameof(model.GuestCount), $"โต๊ะนี้รองรับได้ไม่เกิน {lockedTable.Capacity} คน");
            model.TableNumber = lockedTable.TableNumber;
            model.TableCapacity = lockedTable.Capacity;
            return View(model);
        }

        var hasConflict = _context.TableReservations.Any(reservation =>
            reservation.TableID == lockedTable.TableID &&
            reservation.Status != ReservationStatuses.Cancelled &&
            reservation.Status != ReservationStatuses.Completed &&
            reservation.ReservationDateTime < reservationEnd &&
            reservation.ReservationEndTime > reservationStart);
        if (hasConflict)
        {
            ModelState.AddModelError(string.Empty, "โต๊ะนี้มีผู้จองในช่วงเวลาดังกล่าวแล้ว กรุณาเลือกเวลาอื่นหรือกลับไปเลือกโต๊ะใหม่");
            return View(model);
        }

        var reservation = new TableReservation
        {
            UserID = user.UserId,
            TableID = lockedTable.TableID,
            Package = model.Package,
            ReservationDateTime = reservationStart,
            ReservationEndTime = reservationEnd,
            GuestCount = model.GuestCount,
            CustomerName = model.CustomerName.Trim(),
            CustomerEmail = user.Email,
            CustomerPhone = model.CustomerPhone.Trim(),
            Note = string.IsNullOrWhiteSpace(model.Note) ? null : model.Note.Trim(),
            Status = ReservationStatuses.Pending,
            CreatedAt = DateTime.Now
        };
        _context.TableReservations.Add(reservation);
        _context.SaveChanges();
        transaction.Commit();

        return RedirectToAction(nameof(ReservationSuccess), new { id = reservation.ReservationID });
    }

    [Authorize(Roles = AppRoles.Customer)]
    [HttpGet]
    public IActionResult CheckTableAvailability(int tableId, DateTime date, string time)
    {
        var table = GetActiveTable(tableId);
        if (table == null)
        {
            return Json(new { available = false, message = "ไม่พบโต๊ะหรือโต๊ะปิดใช้งาน" });
        }

        var start = GetReservationStart(date, time);
        if (!start.HasValue)
        {
            return Json(new { available = false, message = "กรุณาเลือกวันที่และเวลา" });
        }
        var end = start.Value.AddHours(2);
        var conflict = _context.TableReservations.Any(reservation =>
            reservation.TableID == tableId &&
            reservation.Status != ReservationStatuses.Cancelled &&
            reservation.Status != ReservationStatuses.Completed &&
            reservation.ReservationDateTime < end &&
            reservation.ReservationEndTime > start.Value);

        return Json(conflict
            ? new { available = false, message = "โต๊ะนี้ไม่ว่างในช่วงเวลาที่เลือก กรุณาเลือกวันหรือเวลาอื่น" }
            : new { available = true, message = "โต๊ะนี้ว่างในช่วงเวลาที่เลือก" });
    }

    [Authorize(Roles = AppRoles.Customer)]
    public IActionResult ReservationSuccess(int id)
    {
        var user = GetCurrentCustomer();
        if (user == null) return Challenge();

        var reservation = (from item in _context.TableReservations.AsNoTracking()
                           join table in _context.DiningTables.AsNoTracking()
                               on item.TableID equals table.TableID
                           where item.ReservationID == id && item.UserID == user.UserId
                           select new ReservationConfirmationViewModel
                           {
                               ReservationId = item.ReservationID,
                               TableId = table.TableID,
                               TableNumber = table.TableNumber,
                               ReservationDateTime = item.ReservationDateTime,
                               GuestCount = item.GuestCount,
                               Package = item.Package,
                               Status = item.Status
                           })
            .SingleOrDefault();
        if (reservation == null) return NotFound();

        return View(reservation);
    }

    [Authorize(Roles = AppRoles.Customer)]
    public IActionResult ReservationHistory()
    {
        var user = GetCurrentCustomer();
        if (user == null) return Challenge();

        var reservations = (from reservation in _context.TableReservations.AsNoTracking()
                            join table in _context.DiningTables.AsNoTracking()
                                on reservation.TableID equals table.TableID
                            where reservation.UserID == user.UserId
                            orderby reservation.ReservationDateTime descending
                            select new ReservationHistoryItemViewModel
                            {
                                ReservationId = reservation.ReservationID,
                                TableId = table.TableID,
                                TableNumber = table.TableNumber,
                                ReservationDateTime = reservation.ReservationDateTime,
                                GuestCount = reservation.GuestCount,
                                Package = reservation.Package,
                                Status = reservation.Status
                            })
            .ToList();

        return View(reservations);
    }

    [Authorize(Roles = AppRoles.Customer)]
    public IActionResult ReservationDetail(int id)
    {
        var user = GetCurrentCustomer();
        if (user == null) return Challenge();

        var reservation = (from item in _context.TableReservations.AsNoTracking()
                           join table in _context.DiningTables.AsNoTracking()
                               on item.TableID equals table.TableID
                           where item.ReservationID == id && item.UserID == user.UserId
                           select new ReservationDetailsDisplayViewModel
                           {
                               ReservationId = item.ReservationID,
                               TableId = table.TableID,
                               TableNumber = table.TableNumber,
                               ReservationDateTime = item.ReservationDateTime,
                               GuestCount = item.GuestCount,
                               Package = item.Package,
                               Status = item.Status,
                               CustomerName = item.CustomerName,
                               CustomerEmail = item.CustomerEmail,
                               CustomerPhone = item.CustomerPhone,
                               Note = item.Note
                           })
            .SingleOrDefault();
        if (reservation == null) return NotFound();

        return View(reservation);
    }

    public IActionResult ScanQR(int? tableId, int? guestCount)
    {
        var table = GetActiveTable(tableId);
        if (tableId.HasValue && table == null) return BadRequest();
        if (guestCount.HasValue &&
            (table == null || guestCount.Value < 1 || guestCount.Value > table.Capacity))
        {
            return BadRequest();
        }

        ViewBag.TableId = table?.TableID;
        ViewBag.TableNumber = table?.TableNumber;
        ViewBag.TableCapacity = table?.Capacity;
        ViewBag.GuestCount = guestCount ?? 1;
        return View();
    }

    [Authorize(Roles = AppRoles.Customer)]
    [HttpGet]
    public IActionResult BuffetCheckout(
        int tableId,
        int guestCount,
        string? promoCode,
        int buffetFreeChildCount = 0,
        string? generalPromoCode = null,
        int? childAge = null,
        int? childHeightCm = null)
    {
        var table = GetActiveTable(tableId);
        if (table == null) return BadRequest();
        if (guestCount < 1 || guestCount > table.Capacity) return BadRequest();

        var childPromotionConfig = _context.BuffetChildPromotions
            .AsNoTracking()
            .Single(promotion => promotion.PromotionId == 1);
        ViewBag.MaxFreeChildren = childPromotionConfig.MaxFreeChildren;
        ViewBag.MaxChildAge = childPromotionConfig.MaxChildAge;
        ViewBag.MaxChildHeightCm = childPromotionConfig.MaxChildHeightCm;

        var discountAmount = 0m;
        if (!TryCalculateBuffetChildDiscount(
                guestCount,
                promoCode,
                buffetFreeChildCount,
                childAge,
                childHeightCm,
                out discountAmount,
                out var promoError))
        {
            ViewBag.PromoError = promoError;
            discountAmount = 0m;
        }

        var subtotalAfterChildPromotion = BuffetPricePerPerson * guestCount - discountAmount;
        var generalDiscountAmount = 0m;
        var user = GetCurrentCustomer();
        string? generalPromoError = null;
        if (!string.IsNullOrWhiteSpace(generalPromoCode) &&
            TryCalculateGeneralPromotion(
                generalPromoCode,
                "Buffet",
                subtotalAfterChildPromotion,
                user,
                null,
                out generalDiscountAmount,
                out _,
                out generalPromoError))
        {
            ViewBag.GeneralPromoCode = generalPromoCode.Trim().ToUpperInvariant();
        }
        else
        {
            generalDiscountAmount = 0m;
            ViewBag.GeneralPromoCode = generalPromoCode?.Trim();
            if (!string.IsNullOrWhiteSpace(generalPromoCode))
            {
                ViewBag.GeneralPromoError = generalPromoError;
            }
        }

        ViewBag.DiscountAmount = discountAmount;
        ViewBag.GeneralDiscountAmount = generalDiscountAmount;
        ViewBag.TotalAmount = subtotalAfterChildPromotion - generalDiscountAmount;
        ViewBag.OrderType = BuffetOrderType;
        ViewBag.ArriveTime = "-";
        ViewBag.IsPreorder = false;
        ViewBag.IsBuffet = true;
        ViewBag.BuffetGuestCount = guestCount;
        ViewBag.BuffetFreeChildCount = buffetFreeChildCount;
        ViewBag.ChildAge = childAge;
        ViewBag.ChildHeightCm = childHeightCm;
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
        TableReservation? confirmedReservation = null;
        if (table != null)
        {
            var user = GetCurrentCustomer();
            if (user != null)
            {
                confirmedReservation = _context.TableReservations
                    .AsNoTracking()
                    .Where(reservation =>
                        reservation.TableID == table.TableID &&
                        reservation.UserID == user.UserId &&
                        reservation.Status == ReservationStatuses.Confirmed)
                    .OrderByDescending(reservation => reservation.ReservationDateTime)
                    .FirstOrDefault();
            }
        }
        ViewBag.IsTableOrder = table != null;
        ViewBag.IsBuffet = confirmedReservation?.Package == "buffet";
        ViewBag.BuffetGuestCount = confirmedReservation?.GuestCount;
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

        var menuIds = menuList.Select(menu => menu.MenuId).ToList();
        var menuOrderItems = _context.OrderItems
            .AsNoTracking()
            .Where(item => menuIds.Contains(item.MenuID))
            .AsEnumerable()
            .ToList();
        var orderIdsByMenu = menuOrderItems
            .GroupBy(item => item.MenuID)
            .ToDictionary(
                group => group.Key,
                group => group.Select(item => item.OrderID).Distinct().ToList());

        var reviews = _context.OrderReviews
            .AsNoTracking()
            .Where(review => !review.IsHidden)
            .ToList();
        var reviewStats = new Dictionary<int, (double Average, int Count)>();
        foreach (var menuEntry in orderIdsByMenu)
        {
            var ratings = reviews
                .Where(review => menuEntry.Value.Contains(review.OrderId))
                .Select(review => review.Rating)
                .ToList();
            if (ratings.Count > 0)
            {
                reviewStats[menuEntry.Key] =
                    (ratings.Average(), ratings.Count);
            }
        }
        ViewBag.ReviewStats = reviewStats;

        return View(menuList);
    }

    public IActionResult FoodDetail(int id, int? tableId, bool viewOnly = false, bool returnToMenu = false)
    {
        var table = GetActiveTable(tableId);
        if (tableId.HasValue && table == null) return BadRequest();

        var menu = _context.Menus.FirstOrDefault(m => m.MenuId == id);
        if (menu == null) return RedirectToAction("Menu");
        menu.ImagePath = MenuImageStorage.GetPublicPath(_environment.WebRootPath, menu.MenuId);
        ViewBag.TableId = table?.TableID;
        ViewBag.TableNumber = table?.TableNumber;
        ViewBag.IsViewOnly = viewOnly;
        ViewBag.ReturnToMenu = returnToMenu;
        ViewBag.IsBuffet = table != null
            ? GetConfirmedBuffetReservation(table.TableID) != null
            : false;
        var menuOrderIds = _context.OrderItems
            .AsNoTracking()
            .Where(item => item.MenuID == id)
            .Select(item => item.OrderID)
            .Distinct()
            .ToList();
        var menuReviews = _context.OrderReviews
            .AsNoTracking()
            .Include(review => review.User)
            .Where(review =>
                !review.IsHidden &&
                menuOrderIds.Contains(review.OrderId))
            .OrderByDescending(review => review.CreatedAt)
            .ToList();
        ViewBag.MenuReviews = menuReviews;
        ViewBag.MenuReviewAverage = menuReviews.Count > 0
            ? menuReviews.Average(review => review.Rating)
            : 0d;
        ViewBag.MenuReviewCount = menuReviews.Count;
        return View(menu);
    }

    // --- ฟังก์ชันเกี่ยวกับการสั่งอาหาร (ต้อง Login ก่อน) ---

    [Authorize(Roles = AppRoles.Customer)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddToCart(int menuId, int qty, int? tableId, string? specialRequest)
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

        var normalizedRequest = string.IsNullOrWhiteSpace(specialRequest)
            ? null
            : specialRequest.Trim();
        if (normalizedRequest?.Length > 500)
        {
            TempData["OrderError"] = "หมายเหตุต้องไม่เกิน 500 ตัวอักษร";
            return RedirectToAction(nameof(FoodDetail), new { id = menuId, tableId });
        }

        var cart = GetCart();
        var existingItem = cart.FirstOrDefault(c =>
            c.MenuId == menuId &&
            string.Equals(c.SpecialRequest, normalizedRequest, StringComparison.Ordinal));
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
            cart.Add(new CartItem
            {
                MenuId = menu.MenuId,
                MenuName = menu.MenuName,
                Price = menu.Price,
                Qty = qty,
                SpecialRequest = normalizedRequest
            });
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
        var confirmedBuffetReservation = table != null
            ? GetConfirmedBuffetReservation(table.TableID)
            : null;
        ViewBag.IsBuffet = confirmedBuffetReservation != null;
        ViewBag.BuffetGuestCount = confirmedBuffetReservation?.GuestCount;
        ViewBag.BuffetTotalAmount = confirmedBuffetReservation != null
            ? BuffetPricePerPerson * confirmedBuffetReservation.GuestCount
            : 0m;
        var cart = GetCart();
        foreach (var item in cart)
        {
            item.ImagePath = MenuImageStorage.GetPublicPath(_environment.WebRootPath, item.MenuId);
        }

        return View(cart);
    }

    [Authorize(Roles = AppRoles.Customer)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Payment(string? arriveTime, int? tableId, string? generalPromoCode = null)
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
        var confirmedBuffetReservation = table != null
            ? GetConfirmedBuffetReservation(table.TableID)
            : null;
        if (confirmedBuffetReservation != null)
        {
            totalAmount = BuffetPricePerPerson * confirmedBuffetReservation.GuestCount;
        }
        var generalDiscountAmount = 0m;
        if (!string.IsNullOrWhiteSpace(generalPromoCode))
        {
            var user = GetCurrentCustomer();
            string? generalPromoError = null;
            if (TryCalculateGeneralPromotion(
                    generalPromoCode,
                    confirmedBuffetReservation != null ? "Buffet" : "Alacarte",
                    totalAmount,
                    user,
                    GetCart(),
                    out generalDiscountAmount,
                    out _,
                    out generalPromoError))
            {
                ViewBag.GeneralPromoCode = generalPromoCode.Trim().ToUpperInvariant();
            }
            else
            {
                generalDiscountAmount = 0m;
                ViewBag.GeneralPromoCode = generalPromoCode.Trim();
                ViewBag.GeneralPromoError = generalPromoError;
            }
        }
        ViewBag.GeneralDiscountAmount = generalDiscountAmount;
        ViewBag.TotalAmount = totalAmount - generalDiscountAmount;
        ViewBag.OrderType = confirmedBuffetReservation != null
            ? BuffetOrderType
            : isPreorder
                ? PreorderPickupOrderType
                : DineInOrderType;
        ViewBag.ArriveTime = isPreorder ? arriveTime : "-";
        ViewBag.IsPreorder = isPreorder;
        ViewBag.IsBuffet = confirmedBuffetReservation != null;
        ViewBag.BuffetGuestCount = confirmedBuffetReservation?.GuestCount;
        ViewBag.BuffetFreeChildCount = 0;
        ViewBag.PromoCode = null;
        ViewBag.BuffetPricePerPerson = BuffetPricePerPerson;
        ViewBag.DiscountAmount = 0m;
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
        int? childAge,
        int? childHeightCm,
        string? generalPromoCode,
        IFormFile? paymentProof)
    {
        if (User.Identity == null || !User.Identity.IsAuthenticated) return RedirectToAction("Login", "Account");
        const string cashMethod = "เงินสด (ชำระที่หน้าร้าน)";
        const string promptPayMethod = "PromptPay";

        var isBuffet = buffetGuestCount.HasValue;
        var guestCount = buffetGuestCount.GetValueOrDefault();
        var freeChildCount = buffetFreeChildCount.GetValueOrDefault();
        var normalizedPromoCode = string.IsNullOrWhiteSpace(promoCode) ? null : promoCode.Trim();
        var normalizedGeneralPromoCode = string.IsNullOrWhiteSpace(generalPromoCode)
            ? null
            : generalPromoCode.Trim();
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
                    childAge,
                    childHeightCm,
                    out discountAmount,
                    out var promoError))
            {
                TempData["OrderError"] = promoError;
                return RedirectToAction(nameof(BuffetCheckout), new
                {
                    tableId,
                    guestCount,
                    promoCode = normalizedPromoCode,
                    buffetFreeChildCount = freeChildCount,
                    childAge,
                    childHeightCm
                });
            }
        }
        else if (normalizedPromoCode != null || freeChildCount != 0)
        {
            return BadRequest();
        }

        var cart = GetCart();
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

        var subtotalAmount = isBuffet
            ? BuffetPricePerPerson * guestCount - discountAmount
            : cart.Sum(item => menus[item.MenuId].Price * item.Qty);
        var generalDiscountAmount = 0m;
        GeneralPromotion? generalPromotion = null;
        if (normalizedGeneralPromoCode != null)
        {
            string? generalPromoError = null;
            if (!TryCalculateGeneralPromotion(
                    normalizedGeneralPromoCode,
                    isBuffet ? "Buffet" : "Alacarte",
                    subtotalAmount,
                    user,
                    cart,
                    out generalDiscountAmount,
                    out generalPromotion,
                    out generalPromoError))
            {
                TempData["OrderError"] = generalPromoError;
                if (isBuffet)
                {
                    return RedirectToAction(nameof(BuffetCheckout), new
                    {
                        tableId,
                        guestCount,
                        promoCode = normalizedPromoCode,
                        buffetFreeChildCount = freeChildCount,
                        childAge,
                        childHeightCm,
                        generalPromoCode = normalizedGeneralPromoCode
                    });
                }
                return RedirectToAction(nameof(Cart), new { tableId });
            }

            if (generalPromotion == null)
            {
                TempData["OrderError"] = "ไม่พบโค้ดโปรโมชั่น";
                return RedirectToAction(nameof(Cart), new { tableId });
            }

            var lockedPromotion = _context.GeneralPromotions
                .FromSqlInterpolated(
                    $"SELECT * FROM `general_promotions` WHERE `PromotionID` = {generalPromotion.PromotionId} FOR UPDATE")
                .AsEnumerable()
                .SingleOrDefault();
            if (lockedPromotion == null ||
                !lockedPromotion.IsActive ||
                (lockedPromotion.StartDate.HasValue && DateTime.Today < lockedPromotion.StartDate.Value.Date) ||
                (lockedPromotion.EndDate.HasValue && DateTime.Today > lockedPromotion.EndDate.Value.Date))
            {
                TempData["OrderError"] = "โค้ดโปรโมชั่นไม่สามารถใช้ได้ในตอนนี้";
                return RedirectToAction(nameof(Cart), new { tableId });
            }
            if (lockedPromotion.MaxUses.HasValue &&
                lockedPromotion.UsedCount >= lockedPromotion.MaxUses.Value)
            {
                TempData["OrderError"] = "โค้ดโปรโมชั่นนี้ใช้ครบจำนวนครั้งที่กำหนดแล้ว";
                return RedirectToAction(nameof(Cart), new { tableId });
            }
            if (lockedPromotion.FirstOrderOnly &&
                _context.Orders.AsNoTracking().Any(order =>
                    order.UserID == user.UserId &&
                    order.OrderStatus != "ยกเลิก"))
            {
                TempData["OrderError"] = "โค้ดนี้ใช้ได้เฉพาะออเดอร์แรกของสมาชิกเท่านั้น";
                return RedirectToAction(nameof(Cart), new { tableId });
            }
            if (lockedPromotion.MaxUsesPerUser.HasValue)
            {
                var lockedPromotionId = lockedPromotion.PromotionId;
                var lockedUserId = user.UserId;
                var perUserCount = _context.GeneralPromotionUsages
                    .AsNoTracking()
                    .Count(usage =>
                        usage.PromotionId == lockedPromotionId &&
                        usage.UserId == lockedUserId);
                if (perUserCount >= lockedPromotion.MaxUsesPerUser.Value)
                {
                    TempData["OrderError"] = "โค้ดนี้ใช้ครบจำนวนครั้งที่กำหนดต่อสมาชิกแล้ว";
                    return RedirectToAction(nameof(Cart), new { tableId });
                }
            }
            generalPromotion = lockedPromotion;

            if (!TryCalculateGeneralPromotion(
                    normalizedGeneralPromoCode,
                    isBuffet ? "Buffet" : "Alacarte",
                    subtotalAmount,
                    user,
                    cart,
                    out generalDiscountAmount,
                    out generalPromotion,
                    out generalPromoError))
            {
                TempData["OrderError"] = generalPromoError;
                return RedirectToAction(nameof(Cart), new { tableId });
            }
        }

        decimal totalAmount = subtotalAmount - generalDiscountAmount;

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
                    buffetFreeChildCount = freeChildCount,
                    childAge,
                    childHeightCm
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
            PromoCode = generalPromotion != null
                ? generalPromotion.Code
                : isBuffet && freeChildCount > 0
                    ? normalizedPromoCode
                    : null,
            DiscountAmount = discountAmount + generalDiscountAmount,
            TotalAmount = totalAmount,
            OrderStatus = "รอดำเนินการ"
        };

        _context.Orders.Add(newOrder);
        await _context.SaveChangesAsync(HttpContext.RequestAborted);
        newOrder.DisplayOrderNumber = $"ORDER{newOrder.OrderID:D4}";
        await _context.SaveChangesAsync(HttpContext.RequestAborted);

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
                StockDeducted = stockDeducted,
                SpecialRequest = item.SpecialRequest
            };
            _context.OrderItems.Add(orderItem);
        }
        await _context.SaveChangesAsync(HttpContext.RequestAborted);

        if (generalPromotion != null)
        {
            var promotionToUpdate = _context.GeneralPromotions
                .Single(promotion => promotion.PromotionId == generalPromotion.PromotionId);
            if (!generalPromotion.RequiresThemeApproval)
            {
                promotionToUpdate.UsedCount++;
            }
            _context.GeneralPromotionUsages.Add(new GeneralPromotionUsage
            {
                PromotionId = generalPromotion.PromotionId,
                UserId = user.UserId,
                OrderId = newOrder.OrderID,
                UsedAt = DateTime.Now,
                RequiresThemeApproval = generalPromotion.RequiresThemeApproval,
                ThemeApprovalStatus = generalPromotion.RequiresThemeApproval
                    ? "รออนุมัติ"
                    : ""
            });
            await _context.SaveChangesAsync(HttpContext.RequestAborted);
        }

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
        ClearCart();

        return RedirectToAction("OrderHistory");  
    }

    [Authorize(Roles = AppRoles.Customer)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CancelOrder(int id)
    {
        var userEmail = User.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(userEmail))
        {
            return Challenge();
        }

        var user = _context.Users.SingleOrDefault(account => account.Email == userEmail);
        if (user == null)
        {
            return Challenge();
        }

        using var transaction = _context.Database.BeginTransaction();
        var order = _context.Orders
            .SingleOrDefault(item => item.OrderID == id && item.UserID == user.UserId);
        if (order == null)
        {
            return NotFound();
        }
        if (order.OrderStatus is "กำลังเตรียม" or "พร้อมรับ" or "เสร็จแล้ว" or "ยกเลิก")
        {
            TempData["PaymentError"] = "ออเดอร์อยู่ในขั้นตอนที่ไม่สามารถยกเลิกได้";
            return RedirectToAction(nameof(OrderDetails), new { id });
        }
        if (order.OrderStatus != "รอดำเนินการ" &&
            order.OrderStatus != "ยืนยันแล้ว")
        {
            TempData["PaymentError"] = "ออเดอร์อยู่ในสถานะที่ลูกค้ายกเลิกไม่ได้";
            return RedirectToAction(nameof(OrderDetails), new { id });
        }

        if (order.OrderType == PreorderPickupOrderType &&
            TimeSpan.TryParse(order.ArriveTime, out var pickupTime))
        {
            var pickupDeadline = DateTime.Today.Add(pickupTime);
            if (pickupDeadline <= DateTime.Now.AddMinutes(30))
            {
                TempData["PaymentError"] = "ต้องยกเลิกสั่งล่วงหน้าอย่างน้อย 30 นาทีก่อนเวลารับอาหาร";
                return RedirectToAction(nameof(OrderDetails), new { id });
            }
        }

        var orderItems = _context.OrderItems
            .Where(item => item.OrderID == order.OrderID && item.StockDeducted)
            .ToList();
        foreach (var item in orderItems)
        {
            var menu = _context.Menus
                .FromSqlInterpolated($"SELECT * FROM `menus` WHERE `MenuID` = {item.MenuID} FOR UPDATE")
                .AsEnumerable()
                .SingleOrDefault();
            if (menu?.StockQuantity is int stockQuantity)
            {
                menu.StockQuantity = stockQuantity + item.Quantity;
            }
            item.StockDeducted = false;
        }

        if (!string.IsNullOrWhiteSpace(order.PromoCode))
        {
            var usedPromotion = _context.GeneralPromotions
                .SingleOrDefault(promotion =>
                    promotion.Code == order.PromoCode.ToUpperInvariant());
            if (usedPromotion is { UsedCount: > 0 })
            {
                usedPromotion.UsedCount--;
            }
            var usage = _context.GeneralPromotionUsages
                .SingleOrDefault(item => item.OrderId == order.OrderID);
            if (usage != null)
            {
                _context.GeneralPromotionUsages.Remove(usage);
            }
        }
        else
        {
            var usage = _context.GeneralPromotionUsages
                .SingleOrDefault(item => item.OrderId == order.OrderID);
            if (usage != null)
            {
                var usedPromotion = _context.GeneralPromotions
                    .SingleOrDefault(promotion =>
                        promotion.PromotionId == usage.PromotionId);
                if (usedPromotion is { UsedCount: > 0 })
                {
                    usedPromotion.UsedCount--;
                }
                _context.GeneralPromotionUsages.Remove(usage);
            }
        }

        if (order.TableID.HasValue)
        {
            var reservation = _context.TableReservations
                .Where(item =>
                    item.TableID == order.TableID &&
                    item.Status != ReservationStatuses.Cancelled &&
                    item.Status != ReservationStatuses.Completed &&
                    item.Package == "buffet" &&
                    item.GuestCount == order.BuffetGuestCount)
                .OrderBy(item => item.ReservationDateTime)
                .FirstOrDefault();
            if (reservation != null)
            {
                reservation.Status = ReservationStatuses.Cancelled;
            }
        }

        order.OrderStatus = "ยกเลิก";
        _context.SaveChanges();
        transaction.Commit();

        TempData["PaymentMessage"] = "ยกเลิกออเดอร์เรียบร้อยแล้ว";
        return RedirectToAction(nameof(OrderDetails), new { id });
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

        var customerCanCancel =
            order.OrderStatus is "รอดำเนินการ" or "ยืนยันแล้ว";
        if (customerCanCancel &&
            order.OrderType == PreorderPickupOrderType &&
            TimeSpan.TryParse(order.ArriveTime, out var pickupTime))
        {
            customerCanCancel =
                DateTime.Today.Add(pickupTime) > DateTime.Now.AddMinutes(30);
        }
        ViewBag.CustomerCanCancel = customerCanCancel;
        ViewBag.CanReview = order.OrderStatus == "เสร็จแล้ว";
        ViewBag.ExistingReview = _context.OrderReviews
            .AsNoTracking()
            .SingleOrDefault(review =>
                review.OrderId == order.OrderID &&
                review.UserId == user.UserId &&
                !review.IsHidden);

        var items = (from orderItem in _context.OrderItems.AsNoTracking()
                     join menu in _context.Menus.AsNoTracking()
                         on orderItem.MenuID equals menu.MenuId
                     where orderItem.OrderID == order.OrderID
                     select new OrderDetailItemViewModel
                     {
                         MenuName = menu.MenuName,
                         Quantity = orderItem.Quantity,
                         UnitPrice = order.BuffetGuestCount.HasValue ? 0m : orderItem.Price,
                         SpecialRequest = orderItem.SpecialRequest
                     }).ToList();

        return View(new OrderDetailViewModel
        {
            Order = order,
            Items = items
        });
    }

    [Authorize(Roles = AppRoles.Customer)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SaveReview(int id, int rating, string? comment)
    {
        if (rating < 1 || rating > 5)
        {
            return BadRequest();
        }

        var userEmail = User.Claims
            .FirstOrDefault(claim => claim.Type == ClaimTypes.Email)?
            .Value;
        var user = _context.Users
            .SingleOrDefault(account => account.Email == userEmail);
        if (user == null)
        {
            return Challenge();
        }

        var order = _context.Orders
            .SingleOrDefault(existingOrder =>
                existingOrder.OrderID == id &&
                existingOrder.UserID == user.UserId);
        if (order == null)
        {
            return NotFound();
        }
        if (order.OrderStatus != "เสร็จแล้ว")
        {
            TempData["PaymentError"] = "รีวิวได้เฉพาะออเดอร์ที่เสร็จสิ้นแล้ว";
            return RedirectToAction(nameof(OrderDetails), new { id });
        }

        var normalizedComment = string.IsNullOrWhiteSpace(comment)
            ? null
            : comment.Trim();
        if (normalizedComment?.Length > 1000)
        {
            TempData["PaymentError"] = "ความคิดเห็นต้องไม่เกิน 1000 ตัวอักษร";
            return RedirectToAction(nameof(OrderDetails), new { id });
        }

        var review = _context.OrderReviews
            .SingleOrDefault(existingReview =>
                existingReview.OrderId == id &&
                existingReview.UserId == user.UserId);
        if (review == null)
        {
            review = new OrderReview
            {
                OrderId = id,
                UserId = user.UserId,
                Rating = rating,
                Comment = normalizedComment,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                IsHidden = false
            };
            _context.OrderReviews.Add(review);
            TempData["PaymentMessage"] = "บันทึกรีวิวเรียบร้อยแล้ว";
        }
        else
        {
            if (review.IsHidden)
            {
                TempData["PaymentError"] = "รีวิวนี้ถูกซ่อน ไม่สามารถแก้ไขได้";
                return RedirectToAction(nameof(OrderDetails), new { id });
            }
            review.Rating = rating;
            review.Comment = normalizedComment;
            review.UpdatedAt = DateTime.Now;
            TempData["PaymentMessage"] = "อัปเดตรีวิวเรียบร้อยแล้ว";
        }
        _context.SaveChanges();
        return RedirectToAction(nameof(OrderDetails), new { id });
    }

    private bool TryCalculateGeneralPromotion(
        string? promoCode,
        string packageType,
        decimal subtotal,
        User? user,
        IReadOnlyList<CartItem>? cartItems,
        out decimal discountAmount,
        out GeneralPromotion? promotion,
        out string? errorMessage)
    {
        discountAmount = 0m;
        promotion = null;
        errorMessage = null;

        var normalizedCode = promoCode?.Trim();
        if (string.IsNullOrEmpty(normalizedCode))
        {
            return true;
        }

        promotion = _context.GeneralPromotions
            .AsNoTracking()
            .SingleOrDefault(item =>
                item.Code == normalizedCode.ToUpperInvariant());
        if (promotion == null)
        {
            errorMessage = "รหัสโปรโมชั่นไม่ถูกต้อง";
            return false;
        }
        if (!promotion.IsActive)
        {
            errorMessage = "โค้ดโปรโมชั่นนี้ถูกปิดใช้งาน";
            return false;
        }
        if (promotion.StartDate.HasValue &&
            DateTime.Today < promotion.StartDate.Value.Date)
        {
            errorMessage = "โค้ดโปรโมชั่นยังไม่ถึงช่วงเวลาใช้งาน";
            return false;
        }
        if (promotion.EndDate.HasValue &&
            DateTime.Today > promotion.EndDate.Value.Date)
        {
            errorMessage = "โค้ดโปรโมชั่นหมดอายุแล้ว";
            return false;
        }
        if (!string.Equals(promotion.AppliesTo, "Both", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(promotion.AppliesTo, packageType, StringComparison.OrdinalIgnoreCase))
        {
            errorMessage = packageType == "Buffet"
                ? "โค้ดโปรโมชั่นนี้ใช้เฉพาะอาลาคาร์ต"
                : "โค้ดโปรโมชั่นนี้ใช้เฉพาะบุฟเฟต์";
            return false;
        }
        if (subtotal < promotion.MinSubtotal)
        {
            errorMessage = $"ยอดขั้นต่ำสำหรับโค้ดนี้คือ ฿{promotion.MinSubtotal:N2}";
            return false;
        }
        if (promotion.BirthMonthOnly)
        {
            if (user?.DateOfBirth == null ||
                user.DateOfBirth.Value.Month != DateTime.Today.Month)
            {
                errorMessage = "โค้ดโปรโมชั่นนี้ใช้ได้เฉพาะลูกค้าที่มีเดือนเกิดตรงกับเดือนปัจจุบัน";
                return false;
            }
        }
        if (promotion.FirstOrderOnly)
        {
            if (user == null)
            {
                errorMessage = "กรุณาเข้าสู่ระบบเพื่อใช้สิทธิ์สมาชิกใหม่";
                return false;
            }
            var hasExistingOrder = _context.Orders
                .AsNoTracking()
                .Any(order =>
                    order.UserID == user.UserId &&
                    order.OrderStatus != "ยกเลิก");
            if (hasExistingOrder)
            {
                errorMessage = "โค้ดนี้ใช้ได้เฉพาะออเดอร์แรกของสมาชิกเท่านั้น";
                return false;
            }
        }
        if (promotion.MaxUsesPerUser.HasValue)
        {
            if (user == null)
            {
                errorMessage = "กรุณาเข้าสู่ระบบเพื่อใช้โค้ดโปรโมชั่น";
                return false;
            }
            var promotionId = promotion.PromotionId;
            var userId = user.UserId;
            var userUsageCount = _context.GeneralPromotionUsages
                .AsNoTracking()
                .Count(usage =>
                    usage.PromotionId == promotionId &&
                    usage.UserId == userId);
            if (userUsageCount >= promotion.MaxUsesPerUser.Value)
            {
                errorMessage = "โค้ดนี้ใช้ครบจำนวนครั้งที่กำหนดต่อสมาชิกแล้ว";
                return false;
            }
        }
        if (promotion.MaxUses.HasValue &&
            promotion.UsedCount >= promotion.MaxUses.Value)
        {
            errorMessage = "โค้ดโปรโมชั่นนี้ใช้ครบจำนวนครั้งที่กำหนดแล้ว";
            return false;
        }

        if (promotion.TargetMenuId.HasValue)
        {
            if (!string.Equals(promotion.DiscountType, "Fixed", StringComparison.OrdinalIgnoreCase))
            {
                errorMessage = "โปรโมชั่นเมนูฟรีต้องใช้ประเภทจำนวนบาท";
                return false;
            }
            if (packageType != "Alacarte")
            {
                errorMessage = "โค้ดเมนูฟรีใช้ได้เฉพาะอาลาคาร์ต";
                return false;
            }

            var targetMenuId = promotion.TargetMenuId.Value;
            var targetLine = cartItems?.FirstOrDefault(item =>
                item.MenuId == targetMenuId &&
                item.Qty >= 1);
            if (targetLine == null)
            {
                var targetMenu = _context.Menus
                    .AsNoTracking()
                    .SingleOrDefault(menu => menu.MenuId == targetMenuId);
                errorMessage = targetMenu == null
                    ? "ไม่พบเมนูเป้าหมายของโปรโมชั่น"
                    : $"กรุณาเพิ่มเมนู {targetMenu.MenuName} อย่างน้อย 1 รายการ";
                return false;
            }

            discountAmount = Math.Min(subtotal, targetLine.Price);
            return true;
        }

        if (string.Equals(promotion.DiscountType, "Percent", StringComparison.OrdinalIgnoreCase))
        {
            discountAmount = Math.Min(subtotal, subtotal * promotion.DiscountValue / 100m);
        }
        else
        {
            discountAmount = Math.Min(subtotal, promotion.DiscountValue);
        }

        return discountAmount >= 0m;
    }

    private bool TryCalculateBuffetChildDiscount(
        int guestCount,
        string? promoCode,
        int freeChildCount,
        int? childAge,
        int? childHeightCm,
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

        if (freeChildCount > promotion.MaxFreeChildren)
        {
            errorMessage = $"โค้ดนี้รองรับเด็กฟรีสูงสุด {promotion.MaxFreeChildren} คน";
            return false;
        }
        if (!childAge.HasValue || childAge.Value < 1 || childAge.Value > promotion.MaxChildAge)
        {
            errorMessage = $"อายุเด็กต้องไม่เกิน {promotion.MaxChildAge} ปี";
            return false;
        }
        if (!childHeightCm.HasValue || childHeightCm.Value < 1 || childHeightCm.Value > promotion.MaxChildHeightCm)
        {
            errorMessage = $"ส่วนสูงเด็กต้องไม่เกิน {promotion.MaxChildHeightCm} ซม.";
            return false;
        }

        discountAmount = BuffetPricePerPerson * freeChildCount;
        return true;
    }

    private BuffetChildPromotion? GetActiveBuffetChildPromotion()
    {
        var today = DateTime.Today;
        return _context.BuffetChildPromotions
            .AsNoTracking()
            .SingleOrDefault(promotion =>
                promotion.PromotionId == 1 &&
                promotion.IsActive &&
                promotion.Code != null &&
                promotion.Code != "" &&
                promotion.StartDate != null &&
                promotion.EndDate != null &&
                promotion.StartDate <= today &&
                promotion.EndDate >= today);
    }

    private static bool IsValidPackage(string? package) =>
        package is "buffet" or "alacarte";

    private static bool IsFuturePickupTime(string? arriveTime) =>
        TimeOnly.TryParseExact(arriveTime, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var pickupTime) &&
        pickupTime > TimeOnly.FromDateTime(DateTime.Now);

    private static bool IsMenuAvailable(Menu menu) =>
        string.Equals(menu.Status, "Available", StringComparison.OrdinalIgnoreCase);

    private User? GetCurrentCustomer()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId)) return null;

        return _context.Users.SingleOrDefault(user =>
            user.UserId == userId && user.Role == AppRoles.Customer);
    }

    private DiningTable? GetActiveTable(int? tableId) =>
        tableId.HasValue
            ? _context.DiningTables.SingleOrDefault(table => table.TableID == tableId.Value && table.IsActive)
            : null;

    private static DateTime? GetReservationStart(DateTime? date, string? time)
    {
        if (!date.HasValue || string.IsNullOrWhiteSpace(time))
        {
            return null;
        }

        if (!TimeSpan.TryParse(time, CultureInfo.InvariantCulture, out var parsedTime))
        {
            return null;
        }

        var start = date.Value.Date.Add(parsedTime);
        return start;
    }

    private TableReservation? GetConfirmedBuffetReservation(int tableId)
    {
        var user = GetCurrentCustomer();
        if (user == null) return null;

        return _context.TableReservations
            .AsNoTracking()
            .Where(reservation =>
                reservation.TableID == tableId &&
                reservation.UserID == user.UserId &&
                reservation.Status == ReservationStatuses.Confirmed &&
                reservation.Package == "buffet")
            .OrderByDescending(reservation => reservation.ReservationDateTime)
            .FirstOrDefault();
    }

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