using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json; // ضروري لتحويل التغييرات إلى JSON
using System.Threading;
using System.Threading.Tasks;
using VinceApp.Data.Models;
using VinceApp.Data;

namespace VinceApp.Data
{
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
        public virtual DbSet<UserToken> UserTokens { get; set; }
        public virtual DbSet<AppSetting> AppSettings { get; set; }
        public virtual DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var builder = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

                // ملف المستخدم داخل AppData (يطغى على الأساسي)
                builder.AddJsonFile(VinceApp.Services.AppConfigService.UserConfigPath, optional: true, reloadOnChange: true);

                var configuration = builder.Build();
                var connectionString = configuration.GetConnectionString("DefaultConnection");

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

                // تأكد من أن قاعدة البيانات تعطي قيمة افتراضية للـ IsDeleted
                // entity.Property(e => e.IsDeleted).HasDefaultValue(false); 

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

            modelBuilder.Entity<User>().HasIndex(u => u.Username).IsUnique();
            modelBuilder.Entity<User>().HasIndex(u => u.EmailAddress).IsUnique();


            modelBuilder.Entity<AppSetting>().HasData(
                new AppSetting
                {
                    Id = 1,
                    SmtpServer = "smtp.gmail.com",
                    Port = 587,
                    SenderEmail = "",
                    SenderPassword = "",
                    PrinterName = "Default"
                }
            );

            modelBuilder.Entity<User>()
            .Property(u => u.IsEmailConfirmed)
            .HasDefaultValue(false);


            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);

        // ---------------------------------------------------------
        //  بداية قسم الـ Audit Trail والـ Soft Delete
        // ---------------------------------------------------------

        public override int SaveChanges()
        {
            OnBeforeSaveChanges();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            OnBeforeSaveChanges();
            return await base.SaveChangesAsync(cancellationToken);
        }

        private void OnBeforeSaveChanges()
        {
            ChangeTracker.DetectChanges();
            var auditEntries = new List<AuditLog>();

            // 1. كشف الطلبات التي يتم حذفها منطقياً (Soft Delete) الآن
            var softDeletedOrderIds = ChangeTracker.Entries<Order>()
                .Where(e => e.State == EntityState.Modified &&
                            (e.Property(p => p.isDeleted).CurrentValue as bool? == true) &&
                            (e.Property(p => p.isDeleted).OriginalValue as bool? == false))
                .Select(e => e.Entity.Id)
                .ToHashSet();

            foreach (var entry in ChangeTracker.Entries())
            {
                // تجاهل السجلات غير المهمة
                if (entry.Entity is AuditLog || entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                    continue;

                var tableName = entry.Entity.GetType().Name;
                bool shouldAudit = false;

                // =========================================================
                //  (1) منطق الفلترة (Selection Logic)
                // =========================================================

                if (entry.Entity is User || entry.Entity is AppSetting)
                {
                    shouldAudit = true;
                }
                else if (entry.Entity is Product)
                {
                    // 1. في حال الإضافة
                    if (entry.State == EntityState.Added)
                    {
                        shouldAudit = true;
                    }
                    // 2. في حال الحذف النهائي (Hard Delete)
                    else if (entry.State == EntityState.Deleted)
                    {
                        shouldAudit = true;
                    }
                    // 3. في حال التعديل
                    else if (entry.State == EntityState.Modified)
                    {
                        // نتحقق فقط من تغير السعر (حذفنا الكود الذي يبحث عن isDeleted)
                        bool isPriceChanged = entry.Property("Price").IsModified;

                        // إذا أردت مراقبة الاسم أيضاً يمكنك إضافة:
                        bool isNameChanged = entry.Property("Name").IsModified;

                        // اجعل الشرط يعتمد على السعر فقط
                        if (isPriceChanged || isNameChanged) shouldAudit = true;
                    }
                }
                else if (entry.Entity is Order order)
                {
                    if (order.isSentToKitchen) shouldAudit = true;
                }
                else if (entry.Entity is OrderDetail detail)
                {
                    bool isItemBeingSoftDeleted = entry.State == EntityState.Modified &&
                                                  (entry.Property("isDeleted").CurrentValue as bool? == true) &&
                                                  (entry.Property("isDeleted").OriginalValue as bool? == false);

                    if (isItemBeingSoftDeleted && softDeletedOrderIds.Contains(detail.OrderId))
                    {
                        continue;
                    }

                    var parentOrder = this.Orders.Local.FirstOrDefault(o => o.Id == detail.OrderId);
                    if (parentOrder == null)
                    {
                        parentOrder = this.Orders.Find(detail.OrderId);
                    }

                    if (parentOrder != null && parentOrder.isSentToKitchen)
                    {
                        shouldAudit = true;
                    }
                }

                if (!shouldAudit) continue;

                // =========================================================
                //  (2) بناء السجل
                // =========================================================

                var auditEntry = new AuditLog
                {
                    TableName = tableName,
                    UserFullName = "System",
                    Timestamp = DateTime.Now,
                    Changes = "{}",
                    Action = "Unknown" // <--- قيمة افتراضية لمنع الخطأ القاتل
                };

                try
                {
                    if (!string.IsNullOrEmpty(CurrentUser.FullName)) auditEntry.UserFullName = CurrentUser.FullName;
                }
                catch { }

                var primaryKey = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());
                auditEntry.RecordId = primaryKey?.CurrentValue?.ToString() ?? "New";

                var changesDict = new Dictionary<string, object>();

                // إضافة المعلومات التعريفية
                if (entry.Entity is OrderDetail od) changesDict["_Info"] = $"Item: {od.ProductName} | Qty: {od.Quantity}";
                else if (entry.Entity is Product p) changesDict["_Info"] = $"Product: {p.Name}";
                else if (entry.Entity is Order o) changesDict["_Info"] = $"Order Total: {o.TotalAmount}";

                // =========================================================
                //  (3) تحديد نوع العملية (Action)
                // =========================================================

                if (entry.State == EntityState.Added)
                {
                    auditEntry.Action = "Insert";
                    foreach (var prop in entry.Properties)
                        if (!prop.IsTemporary) changesDict[prop.Metadata.Name] = prop.CurrentValue;
                }
                else if (entry.State == EntityState.Deleted) // <--- هذا هو الجزء المفقود الذي سبب الخطأ
                {
                    auditEntry.Action = "HardDelete";
                    foreach (var prop in entry.Properties)
                        changesDict[prop.Metadata.Name] = prop.OriginalValue;
                }
                else if (entry.State == EntityState.Modified)
                {
                    auditEntry.Action = "Update";

                    var isDeletedProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "isDeleted");
                    if (isDeletedProp != null &&
                       (isDeletedProp.CurrentValue as bool? == true) &&
                       (isDeletedProp.OriginalValue as bool? == false))
                    {
                        auditEntry.Action = "SoftDelete";
                    }

                    foreach (var prop in entry.Properties)
                    {
                        if (entry.Entity is Product)
                        {
                            if (prop.Metadata.Name != "Price" && prop.Metadata.Name != "Name") continue;
                        }

                        if (prop.IsModified)
                        {
                            changesDict[prop.Metadata.Name] = new { Old = prop.OriginalValue, New = prop.CurrentValue };
                        }
                    }
                }

                // حفظ السجل
                if (changesDict.Count > 0)
                {
                    auditEntry.Changes = JsonSerializer.Serialize(changesDict);
                    auditEntries.Add(auditEntry);
                }
            }

            if (auditEntries.Any())
            {
                AuditLogs.AddRange(auditEntries);
            }
        }
    }
}