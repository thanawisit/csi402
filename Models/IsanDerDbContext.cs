using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace IsanDer.Models;

public partial class IsanDerDbContext : DbContext
{
    public IsanDerDbContext(DbContextOptions<IsanDerDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Menu> Menus { get; set; }

    public virtual DbSet<DiningTable> DiningTables { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    // เพิ่ม DbSet สำหรับตาราง order_items
    public virtual DbSet<OrderItem> OrderItems { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<HomePromotion> HomePromotions { get; set; }

    public virtual DbSet<BuffetChildPromotion> BuffetChildPromotions { get; set; }

    public virtual DbSet<GeneralPromotion> GeneralPromotions { get; set; }

    public virtual DbSet<GeneralPromotionUsage> GeneralPromotionUsages { get; set; }

    public virtual DbSet<TableReservation> TableReservations { get; set; }

    public virtual DbSet<OrderReview> OrderReviews { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_unicode_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<Menu>(entity =>
        {
            entity.HasKey(e => e.MenuId).HasName("PRIMARY");
            entity.ToTable("menus");
            entity.Property(e => e.MenuId).HasColumnName("MenuID");
            entity.Property(e => e.MenuName).HasMaxLength(100);
            entity.Property(e => e.Price).HasPrecision(10, 2);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'Available'");
            entity.Property(e => e.StockQuantity);
            entity.Property(e => e.Description).HasMaxLength(500);
        });

        modelBuilder.Entity<DiningTable>(entity =>
        {
            entity.HasKey(e => e.TableID).HasName("PRIMARY");
            entity.ToTable("dining_tables");
            entity.Property(e => e.TableID).HasColumnName("TableID");
            entity.Property(e => e.TableNumber).HasMaxLength(10).IsRequired();
            entity.HasIndex(e => e.TableNumber).IsUnique();
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        // ตั้งค่าโครงสร้างตาราง orders (หัวบิล) แบบใหม่
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderID).HasName("PRIMARY");
            entity.ToTable("orders");

            entity.HasIndex(e => e.UserID, "UserID");
            entity.HasIndex(e => e.TableID, "IX_orders_TableID");

            entity.Property(e => e.OrderID).HasColumnName("OrderID");
            entity.Property(e => e.DisplayOrderNumber)
                .HasColumnName("DisplayOrderNumber")
                .HasMaxLength(20);
            entity.Property(e => e.UserID).HasColumnName("UserID");
            entity.Property(e => e.TableID).HasColumnName("TableID");
            
            entity.Property(e => e.OrderDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
                
            entity.Property(e => e.OrderType).HasMaxLength(50);
            entity.Property(e => e.ArriveTime).HasMaxLength(20);
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);
            entity.Property(e => e.PaymentStatus)
                .HasMaxLength(30)
                .HasDefaultValueSql("'รอชำระเงิน'");
            entity.Property(e => e.BuffetGuestCount);
            entity.Property(e => e.BuffetFreeChildCount).HasDefaultValue(0);
            entity.Property(e => e.PromoCode).HasMaxLength(50);
            entity.Property(e => e.DiscountAmount).HasPrecision(10, 2).HasDefaultValue(0m);
            entity.Property(e => e.TotalAmount).HasPrecision(10, 2);
            
            entity.Property(e => e.OrderStatus)
                .HasMaxLength(50)
                .HasDefaultValueSql("'รอดำเนินการ'");

            entity.HasOne(e => e.DiningTable)
                .WithMany(e => e.Orders)
                .HasForeignKey(e => e.TableID)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.User)
                .WithMany(e => e.Orders)
                .HasForeignKey(e => e.UserID);
        });

        // ตั้งค่าโครงสร้างตาราง order_items (รายละเอียดอาหารในบิล) ที่เพิ่มเข้ามาใหม่
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.OrderItemID).HasName("PRIMARY");
            entity.ToTable("order_items");

            entity.Property(e => e.OrderItemID).HasColumnName("OrderItemID");
            entity.Property(e => e.OrderID).HasColumnName("OrderID");
            entity.Property(e => e.MenuID).HasColumnName("MenuID");
            entity.Property(e => e.Price).HasPrecision(10, 2);
            entity.Property(e => e.StockDeducted).HasDefaultValue(false);
            entity.Property(e => e.SpecialRequest).HasMaxLength(500);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PRIMARY");
            entity.ToTable("users");
            entity.HasIndex(e => e.Email, "Email").IsUnique();
            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.DateOfBirth).HasColumnType("date");
            entity.Property(e => e.MarketingConsent).HasDefaultValue(false);
            entity.Property(e => e.TermsAcceptedAt).HasColumnType("datetime");
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.Role)
                .HasMaxLength(20)
                .HasDefaultValueSql("'Customer'");
            entity.Property(e => e.Username).HasMaxLength(50);
        });

        modelBuilder.Entity<HomePromotion>(entity =>
        {
            entity.HasKey(promotion => promotion.PromotionId).HasName("PRIMARY");
            entity.ToTable("home_promotions");
            entity.HasIndex(promotion => promotion.SlotKey).IsUnique();
            entity.Property(promotion => promotion.PromotionId).HasColumnName("PromotionID");
            entity.Property(promotion => promotion.SlotKey).HasMaxLength(40);
            entity.Property(promotion => promotion.Kicker).HasMaxLength(100);
            entity.Property(promotion => promotion.Headline).HasMaxLength(200);
            entity.Property(promotion => promotion.Description).HasMaxLength(500);
            entity.Property(promotion => promotion.LinkText).HasMaxLength(100);
            entity.Property(promotion => promotion.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<BuffetChildPromotion>(entity =>
        {
            entity.HasKey(promotion => promotion.PromotionId).HasName("PRIMARY");
            entity.ToTable("buffet_child_promotions");
            entity.Property(promotion => promotion.PromotionId).HasColumnName("PromotionID");
            entity.Property(promotion => promotion.Code).HasMaxLength(50);
            entity.Property(promotion => promotion.StartDate).HasColumnType("date");
            entity.Property(promotion => promotion.EndDate).HasColumnType("date");
            entity.Property(promotion => promotion.IsActive).HasDefaultValue(false);
            entity.Property(promotion => promotion.MaxFreeChildren).HasDefaultValue(1);
            entity.Property(promotion => promotion.MaxChildAge).HasDefaultValue(10);
            entity.Property(promotion => promotion.MaxChildHeightCm).HasDefaultValue(120);
        });

        modelBuilder.Entity<GeneralPromotion>(entity =>
        {
            entity.HasKey(promotion => promotion.PromotionId).HasName("PRIMARY");
            entity.ToTable("general_promotions");
            entity.Property(promotion => promotion.PromotionId).HasColumnName("PromotionID");
            entity.Property(promotion => promotion.Code).HasMaxLength(50).IsRequired();
            entity.HasIndex(promotion => promotion.Code).IsUnique();
            entity.Property(promotion => promotion.Name).HasMaxLength(200).IsRequired();
            entity.Property(promotion => promotion.Description).HasMaxLength(500);
            entity.Property(promotion => promotion.DiscountType).HasMaxLength(20).IsRequired();
            entity.Property(promotion => promotion.DiscountValue).HasPrecision(10, 2).HasDefaultValue(0m);
            entity.Property(promotion => promotion.AppliesTo).HasMaxLength(20).IsRequired();
            entity.Property(promotion => promotion.BirthMonthOnly).HasDefaultValue(false);
            entity.Property(promotion => promotion.MinSubtotal).HasPrecision(10, 2).HasDefaultValue(0m);
            entity.Property(promotion => promotion.StartDate).HasColumnType("date");
            entity.Property(promotion => promotion.EndDate).HasColumnType("date");
            entity.Property(promotion => promotion.MaxUses);
            entity.Property(promotion => promotion.FirstOrderOnly).HasDefaultValue(false);
            entity.Property(promotion => promotion.MaxUsesPerUser);
            entity.Property(promotion => promotion.TargetMenuId);
            entity.Property(promotion => promotion.RequiresThemeApproval).HasDefaultValue(false);
            entity.Property(promotion => promotion.RequiresThemeApproval).HasDefaultValue(false);
            entity.Property(promotion => promotion.UsedCount).HasDefaultValue(0);
            entity.Property(promotion => promotion.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<GeneralPromotionUsage>(entity =>
        {
            entity.HasKey(usage => usage.UsageId).HasName("PRIMARY");
            entity.ToTable("general_promotion_usages");
            entity.Property(usage => usage.UsageId).HasColumnName("UsageID");
            entity.Property(usage => usage.PromotionId).HasColumnName("PromotionID");
            entity.Property(usage => usage.UserId).HasColumnName("UserID");
            entity.Property(usage => usage.OrderId).HasColumnName("OrderID");
            entity.Property(usage => usage.UsedAt).HasColumnType("datetime");
            entity.Property(usage => usage.RequiresThemeApproval).HasDefaultValue(false);
            entity.Property(usage => usage.ThemeApprovalStatus).HasMaxLength(30).HasDefaultValue("");
            entity.HasIndex(usage => new { usage.PromotionId, usage.UserId });
            entity.HasIndex(usage => usage.OrderId).IsUnique();
        });

        modelBuilder.Entity<TableReservation>(entity =>
        {
            entity.HasKey(reservation => reservation.ReservationID).HasName("PRIMARY");
            entity.ToTable("table_reservations");
            entity.HasIndex(reservation => new
            {
                reservation.TableID,
                reservation.ReservationDateTime
            });
            entity.HasIndex(reservation => reservation.UserID);
            entity.Property(reservation => reservation.Package).HasMaxLength(20);
            entity.Property(reservation => reservation.ReservationDateTime).HasColumnType("datetime");
            entity.Property(reservation => reservation.ReservationEndTime).HasColumnType("datetime");
            entity.Property(reservation => reservation.CustomerName).HasMaxLength(50);
            entity.Property(reservation => reservation.CustomerEmail).HasMaxLength(100);
            entity.Property(reservation => reservation.CustomerPhone).HasMaxLength(20);
            entity.Property(reservation => reservation.Note).HasMaxLength(500);
            entity.Property(reservation => reservation.Status)
                .HasMaxLength(30)
                .HasDefaultValueSql("'รอยืนยัน'");
            entity.Property(reservation => reservation.CreatedAt)
                .HasColumnType("datetime")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<OrderReview>(entity =>
        {
            entity.HasKey(review => review.ReviewId).HasName("PRIMARY");
            entity.ToTable("order_reviews");
            entity.Property(review => review.ReviewId).HasColumnName("ReviewId");
            entity.Property(review => review.OrderId).HasColumnName("OrderID");
            entity.Property(review => review.UserId).HasColumnName("UserID");
            entity.Property(review => review.Comment).HasMaxLength(1000);
            entity.Property(review => review.CreatedAt).HasColumnType("datetime");
            entity.Property(review => review.UpdatedAt).HasColumnType("datetime");
            entity.Property(review => review.IsHidden).HasDefaultValue(false);
            entity.HasIndex(review => review.OrderId).IsUnique();
            entity.HasIndex(review => review.UserId);
            entity.HasOne(review => review.Order)
                .WithMany()
                .HasForeignKey(review => review.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(review => review.User)
                .WithMany()
                .HasForeignKey(review => review.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}