using Microsoft.AspNetCore.Authentication.Cookies; // 1. เพิ่ม using นี้สำหรับระบบ Cookie
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using IsanDer.Models;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// --- ส่วนที่แก้ไขเพิ่มเติมสำหรับการเชื่อมต่อ TiDB ---
// ดึงค่า Connection String ที่ชื่อ DefaultConnection จากไฟล์ appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// เพิ่มบรรทัดนี้เพื่อเชื่อมต่อกับไฟล์ IsanDerDbContext ของเราโดยใช้ MySQL Provider
builder.Services.AddDbContext<IsanDer.Models.IsanDerDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
// ---------------------------------------------

// 2. เพิ่มตั้งค่าระบบ Authentication (ยืนยันตัวตน) ด้วย Cookie
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login"; // ถ้ายังไม่ Login ให้เด้งไปหน้านี้
        options.AccessDeniedPath = "/Admin/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(1); // ให้ระบบจำการ Login ไว้ 1 วัน
        options.Events.OnValidatePrincipal = async context =>
        {
            var userIdClaim = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return;
            }

            var dbContext = context.HttpContext.RequestServices.GetRequiredService<IsanDerDbContext>();
            var currentRole = await dbContext.Users
                .AsNoTracking()
                .Where(user => user.UserId == userId)
                .Select(user => user.Role)
                .SingleOrDefaultAsync(context.HttpContext.RequestAborted);

            if (!AppRoles.IsValid(currentRole))
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return;
            }

            var identity = context.Principal?.Identity as ClaimsIdentity;
            if (identity == null)
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return;
            }

            if (identity.FindFirst(ClaimTypes.Role)?.Value != currentRole)
            {
                foreach (var roleClaim in identity.FindAll(ClaimTypes.Role).ToArray())
                {
                    identity.RemoveClaim(roleClaim);
                }

                identity.AddClaim(new Claim(ClaimTypes.Role, currentRole!));
                context.ReplacePrincipal(new ClaimsPrincipal(identity));
                context.ShouldRenew = true;
            }
        };
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();

// 3. ต้องเรียก UseAuthentication (ยืนยันตัวตน) ก่อน UseAuthorization (ตรวจสอบสิทธิ์) เสมอ
app.UseAuthentication(); 
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();