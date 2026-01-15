using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using VinceApp.Data.Models;

namespace VinceApp.Data;

public partial class VinceSweetsDbContext : DbContext
{
    public VinceSweetsDbContext()
    {
    }

    public VinceSweetsDbContext(DbContextOptions<VinceSweetsDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderDetail> OrderDetails { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<RestaurantTable> RestaurantTables { get; set; }
    public virtual DbSet<User> Users { get; set; }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            // 1. تحديد مكان الملف (بجانب ملف التشغيل exe)
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory) 
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            // 2. بناء الإعدادات
            IConfigurationRoot configuration = builder.Build();

            // 3. قراءة جملة الاتصال
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            // 4. استخدامها
            optionsBuilder.UseSqlServer(connectionString);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Categori__3214EC07B789A350");

            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Orders__3214EC07460B37B2");

            entity.Property(e => e.OrderDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            
            entity.Property(e => e.TotalAmount)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 0)");

            entity.HasOne(d => d.Table).WithMany(p => p.Orders)
                .HasForeignKey(d => d.TableId)
                .HasConstraintName("FK__Orders__TableId__440B1D61");
        });

        modelBuilder.Entity<OrderDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__OrderDet__3214EC07875E3983");

            entity.Property(e => e.Price).HasColumnType("decimal(18, 0)");
            entity.Property(e => e.ProductName).HasMaxLength(100);
            entity.Property(e => e.Quantity).HasDefaultValue(1);

            entity.HasOne(d => d.Order).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK__OrderDeta__Order__46E78A0C");

            entity.HasOne(d => d.Product).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__OrderDeta__Produ__47DBAE45");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Products__3214EC07C4D9C222");

            entity.Property(e => e.IsKitchenItem).HasDefaultValue(false);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Price).HasColumnType("decimal(18, 0)");

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("FK__Products__Catego__3E52440B");
        });

        modelBuilder.Entity<RestaurantTable>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Restaura__3214EC0742A7C085");

            entity.HasIndex(e => e.TableNumber, "UQ__Restaura__E8E0DB5213BD93AB").IsUnique();

            entity.Property(e => e.Status).HasDefaultValue(0);
            entity.Property(e => e.TableName).HasMaxLength(100);
        });
        string defaultHash = "A6xnQhbz4Vx2HupVJV8GfVU2I8izILRFlp4T+XjHSE8=";

        modelBuilder.Entity<User>().HasData(new User
        {
            Id = 1,
            Username = "admin",
            PasswordHash = defaultHash, // 1234
            SecurityQuestion = "ما هو الكود الافتراضي؟",
            SecurityAnswerHash = defaultHash, // 1234
            Role = "Admin"
        });
        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
