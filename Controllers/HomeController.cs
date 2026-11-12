using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using IsanDer.Models;

namespace IsanDer.Controllers;

public class HomeController : Controller
{
    private readonly IsanDerDbContext _context;

    // รับค่า Database Context ผ่าน Constructor
    public HomeController(IsanDerDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    public IActionResult Buffet()
    {
        return View();
    }

    public IActionResult ReserveTable()
    {
        return View();
    }

    public IActionResult OrderHistory()
    {
        return View();
    }

    public IActionResult Menu()
    {
        // ดึงข้อมูลรายการอาหารทั้งหมดจากตาราง Menus ในฐานข้อมูล
        var menuList = _context.Menus.ToList();
        return View(menuList);
    }

    public IActionResult FoodDetail()
    {
        return View();
    }

    public IActionResult ScanQR()
    {
        return View();
    }

    public IActionResult Cart()
    {
        return View();
    }
}