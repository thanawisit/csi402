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
            var user = await dbContext.Users
                .SingleOrDefaultAsync(account => account.UserId == userId, context.HttpContext.RequestAborted);
            var currentRole = AppRoles.NormalizeRole(user?.Role);

            if (!AppRoles.IsValid(currentRole))
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return;
            }

            if (user != null && user.Role != currentRole)
            {
                user.Role = currentRole!;
                await dbContext.SaveChangesAsync(context.HttpContext.RequestAborted);
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

// Backward-compatible schema setup for databases created before these features were added.
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<IsanDerDbContext>();
    var connection = context.Database.GetDbConnection();
    if (connection.State != System.Data.ConnectionState.Open)
    {
        connection.Open();
    }

    context.Database.ExecuteSqlRaw("""
        CREATE TABLE IF NOT EXISTS users (
            UserID INT NOT NULL AUTO_INCREMENT,
            Username VARCHAR(50) NOT NULL,
            PasswordHash VARCHAR(255) NOT NULL,
            Role VARCHAR(20) NOT NULL DEFAULT 'Customer',
            Email VARCHAR(100) NOT NULL,
            PhoneNumber VARCHAR(20) NULL,
            DateOfBirth DATE NULL,
            MarketingConsent TINYINT(1) NOT NULL DEFAULT 0,
            TermsAcceptedAt DATETIME NULL,
            PRIMARY KEY (UserID),
            UNIQUE KEY UX_users_Email (Email)
        )
        """);

    context.Database.ExecuteSqlRaw("""
        CREATE TABLE IF NOT EXISTS dining_tables (
            TableID INT NOT NULL AUTO_INCREMENT,
            TableNumber VARCHAR(10) NOT NULL,
            Capacity INT NOT NULL,
            IsActive TINYINT(1) NOT NULL DEFAULT 1,
            PRIMARY KEY (TableID),
            UNIQUE KEY UX_dining_tables_TableNumber (TableNumber)
        )
        """);

    context.Database.ExecuteSqlRaw("""
        CREATE TABLE IF NOT EXISTS menus (
            MenuID INT NOT NULL AUTO_INCREMENT,
            MenuName VARCHAR(100) NOT NULL,
            Price DECIMAL(10,2) NOT NULL,
            Status VARCHAR(20) NOT NULL DEFAULT 'Available',
            StockQuantity INT NULL,
            Description VARCHAR(500) NULL,
            PRIMARY KEY (MenuID)
        )
        """);

    using (var menuCountCommand = connection.CreateCommand())
    {
        menuCountCommand.CommandText = "SELECT COUNT(*) FROM menus";
        if (Convert.ToInt32(menuCountCommand.ExecuteScalar()) == 0)
        {
            context.Database.ExecuteSqlRaw("""
                INSERT INTO menus (MenuName, Price, Status, StockQuantity, Description) VALUES
                ('ส้มตำไทย', 69.00, 'Available', NULL, 'ส้มตำไทยรสชาติเปรี้ยวหวานเผ็ดกำลังดี'),
                ('ลาบหมู', 89.00, 'Available', NULL, 'ลาบหมูปรุงรสอีสานแท้'),
                ('ต้มแซ่บกระดูกอ่อน', 129.00, 'Available', NULL, 'ต้มแซ่บกระดูกอ่อนหม้อใหญ่'),
                ('ไก่ย่างวิเชียรบุรี', 119.00, 'Available', NULL, 'ไก่ย่างหอมเครื่องเทศและน้ำจิ้มแจ่ว'),
                ('คอหมูย่าง', 109.00, 'Available', NULL, 'คอหมูย่างนุ่มพร้อมน้ำจิ้มแจ่ว'),
                ('น้ำอัดลม', 25.00, 'Available', NULL, 'เครื่องดื่มเย็นชื่นใจ')
                """);
        }
    }

    context.Database.ExecuteSqlRaw("""
        CREATE TABLE IF NOT EXISTS orders (
            OrderID INT NOT NULL AUTO_INCREMENT,
            DisplayOrderNumber VARCHAR(20) NULL,
            UserID INT NOT NULL,
            TableID INT NULL,
            OrderDate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
            OrderType VARCHAR(50) NOT NULL,
            ArriveTime VARCHAR(20) NOT NULL,
            PaymentMethod VARCHAR(50) NOT NULL,
            PaymentStatus VARCHAR(30) NOT NULL DEFAULT 'รอชำระเงิน',
            BuffetGuestCount INT NULL,
            BuffetFreeChildCount INT NOT NULL DEFAULT 0,
            PromoCode VARCHAR(50) NULL,
            DiscountAmount DECIMAL(10,2) NOT NULL DEFAULT 0,
            TotalAmount DECIMAL(10,2) NOT NULL,
            OrderStatus VARCHAR(50) NOT NULL DEFAULT 'รอดำเนินการ',
            PRIMARY KEY (OrderID),
            UNIQUE KEY UX_orders_DisplayOrderNumber (DisplayOrderNumber),
            KEY IX_orders_UserID (UserID),
            KEY IX_orders_TableID (TableID)
        )
        """);

    context.Database.ExecuteSqlRaw("""
        CREATE TABLE IF NOT EXISTS order_items (
            OrderItemID INT NOT NULL AUTO_INCREMENT,
            OrderID INT NOT NULL,
            MenuID INT NOT NULL,
            Quantity INT NOT NULL,
            Price DECIMAL(10,2) NOT NULL,
            StockDeducted TINYINT(1) NOT NULL DEFAULT 0,
            SpecialRequest VARCHAR(500) NULL,
            PRIMARY KEY (OrderItemID),
            KEY IX_order_items_OrderID (OrderID),
            KEY IX_order_items_MenuID (MenuID)
        )
        """);

    context.Database.ExecuteSqlRaw("""
        CREATE TABLE IF NOT EXISTS home_promotions (
            PromotionID INT NOT NULL AUTO_INCREMENT,
            SlotKey VARCHAR(40) NOT NULL,
            Kicker VARCHAR(100) NOT NULL,
            Headline VARCHAR(200) NOT NULL,
            Description VARCHAR(500) NOT NULL,
            LinkText VARCHAR(100) NOT NULL,
            IsActive TINYINT(1) NOT NULL DEFAULT 1,
            SortOrder INT NOT NULL DEFAULT 0,
            PRIMARY KEY (PromotionID),
            UNIQUE KEY UX_home_promotions_SlotKey (SlotKey)
        )
        """);

    context.Database.ExecuteSqlRaw("""
        CREATE TABLE IF NOT EXISTS buffet_child_promotions (
            PromotionID TINYINT UNSIGNED NOT NULL,
            Code VARCHAR(50) NULL,
            StartDate DATE NULL,
            EndDate DATE NULL,
            IsActive TINYINT(1) NOT NULL DEFAULT 0,
            MaxFreeChildren INT NOT NULL DEFAULT 1,
            MaxChildAge INT NOT NULL DEFAULT 10,
            MaxChildHeightCm INT NOT NULL DEFAULT 120,
            PRIMARY KEY (PromotionID)
        )
        """);

    context.Database.ExecuteSqlRaw("""
        CREATE TABLE IF NOT EXISTS table_reservations (
            ReservationID INT NOT NULL AUTO_INCREMENT,
            UserID INT NOT NULL,
            TableID INT NOT NULL,
            Package VARCHAR(20) NOT NULL,
            ReservationDateTime DATETIME NOT NULL,
            ReservationEndTime DATETIME NOT NULL,
            GuestCount INT NOT NULL,
            CustomerName VARCHAR(100) NOT NULL,
            CustomerEmail VARCHAR(100) NOT NULL,
            CustomerPhone VARCHAR(20) NOT NULL,
            Note VARCHAR(500) NULL,
            Status VARCHAR(30) NOT NULL DEFAULT 'รอยืนยัน',
            CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
            PRIMARY KEY (ReservationID),
            KEY IX_table_reservations_TableID (TableID),
            KEY IX_table_reservations_UserID (UserID)
        )
        """);

    context.Database.ExecuteSqlRaw("""
        CREATE TABLE IF NOT EXISTS general_promotions (
            PromotionID INT NOT NULL AUTO_INCREMENT,
            Code VARCHAR(50) NOT NULL,
            Name VARCHAR(200) NOT NULL,
            Description VARCHAR(500) NULL,
            DiscountType VARCHAR(20) NOT NULL DEFAULT 'Percent',
            DiscountValue DECIMAL(10,2) NOT NULL DEFAULT 0,
            AppliesTo VARCHAR(20) NOT NULL DEFAULT 'Both',
            BirthMonthOnly TINYINT(1) NOT NULL DEFAULT 0,
            FirstOrderOnly TINYINT(1) NOT NULL DEFAULT 0,
            MaxUsesPerUser INT NULL,
            TargetMenuId INT NULL,
            RequiresThemeApproval TINYINT(1) NOT NULL DEFAULT 0,
            MinSubtotal DECIMAL(10,2) NOT NULL DEFAULT 0,
            StartDate DATE NULL,
            EndDate DATE NULL,
            MaxUses INT NULL,
            UsedCount INT NOT NULL DEFAULT 0,
            IsActive TINYINT(1) NOT NULL DEFAULT 1,
            PRIMARY KEY (PromotionID),
            UNIQUE KEY UX_general_promotions_Code (Code)
        )
        """);

    context.Database.ExecuteSqlRaw("""
        CREATE TABLE IF NOT EXISTS general_promotion_usages (
            UsageID INT NOT NULL AUTO_INCREMENT,
            PromotionID INT NOT NULL,
            UserID INT NOT NULL,
            OrderID INT NOT NULL,
            UsedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
            RequiresThemeApproval TINYINT(1) NOT NULL DEFAULT 0,
            ThemeApprovalStatus VARCHAR(30) NOT NULL DEFAULT '',
            PRIMARY KEY (UsageID),
            UNIQUE KEY UX_promotion_usage_order (OrderID),
            KEY IX_promotion_usage_promotion_user (PromotionID, UserID)
        )
        """);

    using (var displayOrderNumberColumnCommand = connection.CreateCommand())
    {
        displayOrderNumberColumnCommand.CommandText = """
            SELECT COUNT(*)
            FROM information_schema.columns
            WHERE table_schema = DATABASE()
              AND table_name = 'orders'
              AND column_name = 'DisplayOrderNumber'
            """;
        if (Convert.ToInt32(displayOrderNumberColumnCommand.ExecuteScalar()) == 0)
        {
            context.Database.ExecuteSqlRaw(
                "ALTER TABLE orders ADD COLUMN DisplayOrderNumber VARCHAR(20) NULL");
        }
    }

    context.Database.ExecuteSqlRaw("""
        CREATE TABLE IF NOT EXISTS order_number_sequences (
            SequenceID TINYINT NOT NULL,
            LastNumber INT NOT NULL DEFAULT 0,
            PRIMARY KEY (SequenceID)
        )
        """);

    var existingOrders = context.Orders
        .OrderBy(order => order.OrderDate)
        .ThenBy(order => order.OrderID)
        .ToList();

    var nextSequenceNumber = 0;
    var usedDisplayNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    foreach (var order in existingOrders)
    {
        if (!string.IsNullOrWhiteSpace(order.DisplayOrderNumber))
        {
            usedDisplayNumbers.Add(order.DisplayOrderNumber);
            var displaySuffix = order.DisplayOrderNumber.StartsWith("ORDER", StringComparison.OrdinalIgnoreCase)
                ? order.DisplayOrderNumber.Substring(5)
                : order.DisplayOrderNumber;
            if (int.TryParse(displaySuffix, out var existingSequenceNumber))
            {
                nextSequenceNumber = Math.Max(nextSequenceNumber, existingSequenceNumber);
            }
        }
    }

    foreach (var order in existingOrders.Where(order =>
                 string.IsNullOrWhiteSpace(order.DisplayOrderNumber)))
    {
        string displayNumber;
        do
        {
            nextSequenceNumber++;
            displayNumber = $"ORDER{nextSequenceNumber:D4}";
        } while (!usedDisplayNumbers.Add(displayNumber));

        order.DisplayOrderNumber = displayNumber;
    }

    if (existingOrders.Any(order =>
            string.IsNullOrWhiteSpace(order.DisplayOrderNumber) == false))
    {
        context.SaveChanges();
    }

    context.Database.ExecuteSqlInterpolated(
        $"INSERT INTO order_number_sequences (SequenceID, LastNumber) VALUES (1, {nextSequenceNumber}) ON DUPLICATE KEY UPDATE LastNumber = GREATEST(LastNumber, VALUES(LastNumber))");

    context.Database.ExecuteSqlRaw("""
        CREATE TABLE IF NOT EXISTS order_reviews (
            ReviewId INT NOT NULL AUTO_INCREMENT,
            OrderID INT NOT NULL,
            UserID INT NOT NULL,
            Rating TINYINT NOT NULL,
            Comment VARCHAR(1000) NULL,
            CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
            UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
            IsHidden TINYINT(1) NOT NULL DEFAULT 0,
            PRIMARY KEY (ReviewId),
            UNIQUE KEY UX_order_reviews_OrderID (OrderID),
            KEY IX_order_reviews_UserID (UserID)
        )
        """);

    using (var displayOrderNumberIndexCommand = connection.CreateCommand())
    {
        displayOrderNumberIndexCommand.CommandText = """
            SELECT COUNT(*)
            FROM information_schema.statistics
            WHERE table_schema = DATABASE()
              AND table_name = 'orders'
              AND index_name = 'UX_orders_DisplayOrderNumber'
            """;
        if (Convert.ToInt32(displayOrderNumberIndexCommand.ExecuteScalar()) == 0)
        {
            context.Database.ExecuteSqlRaw(
                "ALTER TABLE orders ADD UNIQUE KEY UX_orders_DisplayOrderNumber (DisplayOrderNumber)");
        }
    }

    using var promotionTableCommand = connection.CreateCommand();
    promotionTableCommand.CommandText = """
        SELECT COUNT(*)
        FROM information_schema.tables
        WHERE table_schema = DATABASE()
          AND table_name = 'general_promotions'
        """;
    if (Convert.ToInt32(promotionTableCommand.ExecuteScalar()) == 0)
    {
        context.Database.ExecuteSqlRaw("""
            CREATE TABLE general_promotions (
                PromotionID INT NOT NULL AUTO_INCREMENT,
                Code VARCHAR(50) NOT NULL,
                Name VARCHAR(200) NOT NULL,
                Description VARCHAR(500) NULL,
                DiscountType VARCHAR(20) NOT NULL DEFAULT 'Percent',
                DiscountValue DECIMAL(10,2) NOT NULL DEFAULT 0,
                AppliesTo VARCHAR(20) NOT NULL DEFAULT 'Both',
                BirthMonthOnly TINYINT(1) NOT NULL DEFAULT 0,
                FirstOrderOnly TINYINT(1) NOT NULL DEFAULT 0,
                MaxUsesPerUser INT NULL,
                TargetMenuId INT NULL,
                MinSubtotal DECIMAL(10,2) NOT NULL DEFAULT 0,
                StartDate DATE NULL,
                EndDate DATE NULL,
                MaxUses INT NULL,
                UsedCount INT NOT NULL DEFAULT 0,
                IsActive TINYINT(1) NOT NULL DEFAULT 1,
                PRIMARY KEY (PromotionID),
                UNIQUE KEY UX_general_promotions_Code (Code)
            )
            """);
    }

    using (var requiresUsageColumnCommand = connection.CreateCommand())
    {
        requiresUsageColumnCommand.CommandText = """
            SELECT COUNT(*)
            FROM information_schema.columns
            WHERE table_schema = DATABASE()
              AND table_name = 'general_promotion_usages'
              AND column_name = 'RequiresThemeApproval'
            """;
        if (Convert.ToInt32(requiresUsageColumnCommand.ExecuteScalar()) == 0)
        {
            context.Database.ExecuteSqlRaw(
                "ALTER TABLE general_promotion_usages ADD COLUMN RequiresThemeApproval TINYINT(1) NOT NULL DEFAULT 0");
        }
    }

    using (var approvalStatusColumnCommand = connection.CreateCommand())
    {
        approvalStatusColumnCommand.CommandText = """
            SELECT COUNT(*)
            FROM information_schema.columns
            WHERE table_schema = DATABASE()
              AND table_name = 'general_promotion_usages'
              AND column_name = 'ThemeApprovalStatus'
            """;
        if (Convert.ToInt32(approvalStatusColumnCommand.ExecuteScalar()) == 0)
        {
            context.Database.ExecuteSqlRaw(
                "ALTER TABLE general_promotion_usages ADD COLUMN ThemeApprovalStatus VARCHAR(30) NOT NULL DEFAULT ''");
        }
    }

    using (var firstOrderColumnCommand = connection.CreateCommand())
    {
        firstOrderColumnCommand.CommandText = """
            SELECT COUNT(*)
            FROM information_schema.columns
            WHERE table_schema = DATABASE()
              AND table_name = 'general_promotions'
              AND column_name = 'FirstOrderOnly'
            """;
        if (Convert.ToInt32(firstOrderColumnCommand.ExecuteScalar()) == 0)
        {
            context.Database.ExecuteSqlRaw(
                "ALTER TABLE general_promotions ADD COLUMN FirstOrderOnly TINYINT(1) NOT NULL DEFAULT 0");
        }
    }

    using (var maxUsesPerUserColumnCommand = connection.CreateCommand())
    {
        maxUsesPerUserColumnCommand.CommandText = """
            SELECT COUNT(*)
            FROM information_schema.columns
            WHERE table_schema = DATABASE()
              AND table_name = 'general_promotions'
              AND column_name = 'MaxUsesPerUser'
            """;
        if (Convert.ToInt32(maxUsesPerUserColumnCommand.ExecuteScalar()) == 0)
        {
            context.Database.ExecuteSqlRaw(
                "ALTER TABLE general_promotions ADD COLUMN MaxUsesPerUser INT NULL");
        }
    }

    using (var targetMenuColumnCommand = connection.CreateCommand())
    {
        targetMenuColumnCommand.CommandText = """
            SELECT COUNT(*)
            FROM information_schema.columns
            WHERE table_schema = DATABASE()
              AND table_name = 'general_promotions'
              AND column_name = 'TargetMenuId'
            """;
        if (Convert.ToInt32(targetMenuColumnCommand.ExecuteScalar()) == 0)
        {
            context.Database.ExecuteSqlRaw(
                "ALTER TABLE general_promotions ADD COLUMN TargetMenuId INT NULL");
        }
    }

    using (var requiresThemeApprovalColumnCommand = connection.CreateCommand())
    {
        requiresThemeApprovalColumnCommand.CommandText = """
            SELECT COUNT(*)
            FROM information_schema.columns
            WHERE table_schema = DATABASE()
              AND table_name = 'general_promotions'
              AND column_name = 'RequiresThemeApproval'
            """;
        if (Convert.ToInt32(requiresThemeApprovalColumnCommand.ExecuteScalar()) == 0)
        {
            context.Database.ExecuteSqlRaw(
                "ALTER TABLE general_promotions ADD COLUMN RequiresThemeApproval TINYINT(1) NOT NULL DEFAULT 0");
        }
    }

    using (var usageTableCommand = connection.CreateCommand())
    {
        usageTableCommand.CommandText = """
            SELECT COUNT(*)
            FROM information_schema.tables
            WHERE table_schema = DATABASE()
              AND table_name = 'general_promotion_usages'
            """;
        if (Convert.ToInt32(usageTableCommand.ExecuteScalar()) == 0)
        {
            context.Database.ExecuteSqlRaw("""
                CREATE TABLE general_promotion_usages (
                    UsageID INT NOT NULL AUTO_INCREMENT,
                    PromotionID INT NOT NULL,
                    UserID INT NOT NULL,
                    OrderID INT NOT NULL,
                    UsedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    RequiresThemeApproval TINYINT(1) NOT NULL DEFAULT 0,
                    ThemeApprovalStatus VARCHAR(30) NOT NULL DEFAULT '',
                    PRIMARY KEY (UsageID),
                    UNIQUE KEY UX_promotion_usage_order (OrderID),
                    KEY IX_promotion_usage_promotion_user (PromotionID, UserID)
                )
                """);
        }

        context.Database.ExecuteSqlRaw("""
            INSERT IGNORE INTO general_promotion_usages
                (PromotionID, UserID, OrderID, UsedAt)
            SELECT gp.PromotionID, o.UserID, o.OrderID, o.OrderDate
            FROM orders o
            INNER JOIN general_promotions gp
                ON gp.Code = o.PromoCode
            WHERE o.OrderStatus <> 'ยกเลิก'
            """);

        var generalPromotions = context.GeneralPromotions.ToList();
        foreach (var promotion in generalPromotions)
        {
            promotion.UsedCount = context.GeneralPromotionUsages
                .Count(usage => usage.PromotionId == promotion.PromotionId);
        }
        context.SaveChanges();
    }

    using (var menuDescriptionColumnCommand = connection.CreateCommand())
    {
        menuDescriptionColumnCommand.CommandText = """
            SELECT COUNT(*)
            FROM information_schema.columns
            WHERE table_schema = DATABASE()
              AND table_name = 'menus'
              AND column_name = 'Description'
            """;
        if (Convert.ToInt32(menuDescriptionColumnCommand.ExecuteScalar()) == 0)
        {
            context.Database.ExecuteSqlRaw(
                "ALTER TABLE menus ADD COLUMN Description VARCHAR(500) NULL");
        }
    }

    using (var childPromotionTableCommand = connection.CreateCommand())
    {
        childPromotionTableCommand.CommandText = """
            SELECT COUNT(*)
            FROM information_schema.tables
            WHERE table_schema = DATABASE()
              AND table_name = 'buffet_child_promotions'
            """;
        if (Convert.ToInt32(childPromotionTableCommand.ExecuteScalar()) == 1)
        {
            var childColumns = new[]
            {
                ("MaxFreeChildren", "ALTER TABLE buffet_child_promotions ADD COLUMN MaxFreeChildren INT NOT NULL DEFAULT 1"),
                ("MaxChildAge", "ALTER TABLE buffet_child_promotions ADD COLUMN MaxChildAge INT NOT NULL DEFAULT 10"),
                ("MaxChildHeightCm", "ALTER TABLE buffet_child_promotions ADD COLUMN MaxChildHeightCm INT NOT NULL DEFAULT 120")
            };

            foreach (var (columnName, alterSql) in childColumns)
            {
                using var childColumnCommand = connection.CreateCommand();
                childColumnCommand.CommandText = """
                    SELECT COUNT(*)
                    FROM information_schema.columns
                    WHERE table_schema = DATABASE()
                      AND table_name = 'buffet_child_promotions'
                      AND column_name = @columnName
                    """;
                var parameter = childColumnCommand.CreateParameter();
                parameter.ParameterName = "@columnName";
                parameter.Value = columnName;
                childColumnCommand.Parameters.Add(parameter);
                if (Convert.ToInt32(childColumnCommand.ExecuteScalar()) == 0)
                {
                    context.Database.ExecuteSqlRaw(alterSql);
                }
            }
        }
    }

    using var command = connection.CreateCommand();
    command.CommandText = """
        SELECT COUNT(*)
        FROM information_schema.columns
        WHERE table_schema = DATABASE()
          AND table_name = 'order_items'
          AND column_name = 'SpecialRequest'
        """;
    if (Convert.ToInt32(command.ExecuteScalar()) == 0)
    {
        context.Database.ExecuteSqlRaw(
            "ALTER TABLE order_items ADD COLUMN SpecialRequest VARCHAR(500) NULL");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

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