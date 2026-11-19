using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Scaffolding.Internal;

namespace IsanDer.Models;

public partial class IsanDerDbContext : DbContext
{
    public IsanDerDbContext()
    {
    }

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

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseMySql("server=localhost;database=isanderdb;user=root;password=!Urozx147", Microsoft.EntityFrameworkCore.ServerVersion.Parse("26.7.0-mysql"));

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
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}