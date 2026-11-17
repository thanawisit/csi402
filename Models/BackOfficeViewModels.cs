using System.ComponentModel.DataAnnotations;

namespace IsanDer.Models;

public sealed class AdminDashboardViewModel
{
    public int MenuCount { get; set; }
    public int PendingOrderCount { get; set; }
    public int UserCount { get; set; }
    public int StaffCount { get; set; }
    public int CustomerCount { get; set; }
    public int TodayOrderCount { get; set; }
    public decimal TodayOrderTotal { get; set; }
    public IReadOnlyList<Order> RecentOrders { get; set; } = Array.Empty<Order>();
}

public sealed class MenuInputViewModel
{
    public int? MenuId { get; set; }

    [Required(ErrorMessage = "กรุณากรอกชื่อเมนู")]
    [StringLength(100, ErrorMessage = "ชื่อเมนูต้องไม่เกิน 100 ตัวอักษร")]
    [Display(Name = "ชื่อเมนู")]
    public string MenuName { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "100000", ErrorMessage = "ราคาต้องมากกว่า 0")]
    [Display(Name = "ราคา")]
    public decimal Price { get; set; }

    [Required]
    [Display(Name = "สถานะ")]
    public string Status { get; set; } = "Available";
}

public sealed class AdminUserListItemViewModel
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string Role { get; set; } = AppRoles.Customer;
}

public sealed class StockMenuViewModel
{
    public int MenuId { get; set; }
    public string MenuName { get; set; } = string.Empty;
    public int? StockQuantity { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class SalesReportViewModel
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int CompletedOrderCount { get; set; }
    public decimal TotalSales { get; set; }
    public decimal AverageOrderAmount { get; set; }
    public string? ErrorMessage { get; set; }
    public IReadOnlyList<Order> CompletedOrders { get; set; } = Array.Empty<Order>();
}
