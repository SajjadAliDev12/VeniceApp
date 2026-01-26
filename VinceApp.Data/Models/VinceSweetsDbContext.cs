using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using VinceApp.Data.Models;

namespace VinceApp.Data
{
    public partial class VinceSweetsDbContext : DbContext
    {
        public VinceSweetsDbContext() { }
        public VinceSweetsDbContext(DbContextOptions<VinceSweetsDbContext> options) : base(options) { }

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
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

                builder.AddJsonFile(VinceApp.Services.AppConfigService.UserConfigPath, optional: true, reloadOnChange: true);

                var configuration = builder.Build();
                var connectionString = configuration.GetConnectionString("DefaultConnection");
                optionsBuilder.UseSqlServer(connectionString);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ... (نفس كودك الحالي بدون تغيير)
            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);

        // ---------------------------------------------------------
        //  Audit Trail والـ Soft Delete
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

        // ✅ قائمة الحقول المهمة لتغييرات الطلب (Order)
        private static readonly HashSet<string> _orderImportantProps = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "isSentToKitchen",
            "isReady",
            "isServed",
            "isPaid",
            "isDeleted",
            "TableId",
            "TotalAmount",
            "DiscountAmount",
            "ParentOrderId"
        };

        // ✅ قائمة الحقول المهمة لتغييرات تفاصيل الطلب (OrderDetail)
        private static readonly HashSet<string> _orderDetailImportantProps = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Quantity",
            "isDeleted",
            "Price",
            "ProductId",
            "ProductName",
            "OrderId"
        };

        private void OnBeforeSaveChanges()
        {
            var client = VinceApp.Services.AppConfigService.GetClient();
            bool isKitchenClient = client.Equals("KITCHEN", StringComparison.OrdinalIgnoreCase);

            ChangeTracker.DetectChanges();
            var auditEntries = new List<AuditLog>();

            // 1) كشف الطلبات التي يتم حذفها منطقياً (Soft Delete) الآن
            var softDeletedOrderIds = ChangeTracker.Entries<Order>()
                .Where(e => e.State == EntityState.Modified &&
                            (e.Property(p => p.isDeleted).CurrentValue as bool? == true) &&
                            (e.Property(p => p.isDeleted).OriginalValue as bool? == false))
                .Select(e => e.Entity.Id)
                .ToHashSet();

            // 2) تجميع OrderIds الموجودة داخل ChangeTracker (حتى لا نسوي Find داخل اللوب)
            var trackedOrderIds = ChangeTracker.Entries<Order>()
                .Select(e => e.Entity.Id)
                .ToHashSet();

            // 3) OrderIds المطلوبة لتفاصيل الطلب التي ليست tracked
            var neededOrderIds = ChangeTracker.Entries<OrderDetail>()
                .Where(e => e.State != EntityState.Detached && e.State != EntityState.Unchanged)
                .Select(e => e.Entity.OrderId)
                .Distinct()
                .Where(id => !trackedOrderIds.Contains(id))
                .ToList();

            // 4) جلب isSentToKitchen للطلبات المطلوبة مرة واحدة
            var sentToKitchenLookup = new Dictionary<int, bool>();
            if (neededOrderIds.Count > 0)
            {
                sentToKitchenLookup = Orders
                    .AsNoTracking()
                    .Where(o => neededOrderIds.Contains(o.Id))
                    .Select(o => new { o.Id, o.isSentToKitchen })
                    .ToDictionary(x => x.Id, x => x.isSentToKitchen);
            }

            foreach (var entry in ChangeTracker.Entries())
            {
                // تجاهل السجلات غير المهمة
                if (entry.Entity is AuditLog || entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                    continue;

                // ✅ تجاهل كيانات ما نريدها بالـ Audit (اختياري)
                // مثال: Categories / RestaurantTable إذا تسبب ضوضاء:
                // if (entry.Entity is Category || entry.Entity is RestaurantTable) continue;

                bool shouldAudit = false;

                // =========================================================
                // (1) Selection Logic (فلترة ماذا نسجل)
                // =========================================================

                // Users + AppSetting دائمًا مهمين
                if (entry.Entity is User || entry.Entity is AppSetting)
                {
                    shouldAudit = true;
                }
                // Product: سجّل فقط Add/Delete و (Price/Name) إذا Modified
                else if (entry.Entity is Product)
                {
                    if (entry.State == EntityState.Added || entry.State == EntityState.Deleted)
                    {
                        shouldAudit = true;
                    }
                    else if (entry.State == EntityState.Modified)
                    {
                        bool isPriceChanged = entry.Property("Price").IsModified;
                        bool isNameChanged = entry.Property("Name").IsModified;
                        shouldAudit = isPriceChanged || isNameChanged;
                    }
                }
                // Order: سجّل فقط إذا تغيّر شيء مهم فعلًا (ليس مجرد isSentToKitchen == true)
                else if (entry.Entity is Order)
                {
                    if (entry.State == EntityState.Added || entry.State == EntityState.Deleted)
                    {
                        // ❌ المطبخ لا يضيف ولا يحذف طلبات
                        if (!isKitchenClient)
                            shouldAudit = true;
                    }
                    else if (entry.State == EntityState.Modified)
                    {
                        if (isKitchenClient)
                        {
                            // ✅ المطبخ: فقط تغييرات الحالة
                            shouldAudit = entry.Properties.Any(p =>
                                p.IsModified &&
                                (p.Metadata.Name.Equals("isReady", StringComparison.OrdinalIgnoreCase) ||
                                 p.Metadata.Name.Equals("isServed", StringComparison.OrdinalIgnoreCase))
                            );
                        }
                        else
                        {
                            // ✅ POS: منطقك الحالي كما هو
                            shouldAudit = entry.Properties.Any(p =>
                                p.IsModified && _orderImportantProps.Contains(p.Metadata.Name)
                            );
                        }
                    }
                }


                // OrderDetail: سجّل فقط عمليات مهمة + بشرط أن الطلب تم إرساله للمطبخ (اختياري حسب رغبتك)
                else if (entry.Entity is OrderDetail detail)
                {
                    if (isKitchenClient)
                        continue;

                    // تجاهل حذف عناصر الطلب الذي صار بسبب حذف الطلب نفسه (حتى ما يتكرر)
                    bool isItemBeingSoftDeleted = entry.State == EntityState.Modified &&
                                                  (entry.Property("isDeleted").CurrentValue as bool? == true) &&
                                                  (entry.Property("isDeleted").OriginalValue as bool? == false);

                    if (isItemBeingSoftDeleted && softDeletedOrderIds.Contains(detail.OrderId))
                        continue;

                    // ✅ قرر هل تربط تسجيل تفاصيل الطلب بشرط isSentToKitchen أم لا
                    // إذا تريد منع “ضوضاء المطبخ” تحديدًا:
                    // خليه يسجّل فقط قبل الإرسال أو عند الإرسال من POS.
                    // أنا هنا أخليه يسجّل فقط إذا الطلب isSentToKitchen=true (مثل منطقك) لكن مع تقييد الحقول.
                    bool isSentToKitchen = false;

                    // إذا الطلب tracked (موجود في الذاكرة)
                    var trackedOrderEntry = ChangeTracker.Entries<Order>().FirstOrDefault(o => o.Entity.Id == detail.OrderId);
                    if (trackedOrderEntry != null)
                        isSentToKitchen = trackedOrderEntry.Entity.isSentToKitchen;
                    else if (sentToKitchenLookup.TryGetValue(detail.OrderId, out bool v))
                        isSentToKitchen = v;

                    if (!isSentToKitchen)
                    {
                        // إذا تريد تسجيل تغييرات OrderDetail حتى قبل الإرسال، غيّر هذا إلى (true).
                        continue;
                    }

                    if (entry.State == EntityState.Added || entry.State == EntityState.Deleted)
                    {
                        shouldAudit = true;
                    }
                    else if (entry.State == EntityState.Modified)
                    {
                        // ✅ فقط إذا تغيّرت خواص مهمة (Quantity / isDeleted / Price ...)
                        shouldAudit = entry.Properties.Any(p => p.IsModified && _orderDetailImportantProps.Contains(p.Metadata.Name));
                    }
                }

                if (!shouldAudit) continue;

                // =========================================================
                // (2) بناء السجل
                // =========================================================
                string tableName = entry.Entity.GetType().Name;

                var auditEntry = new AuditLog
                {
                    TableName = tableName,
                    UserFullName = "System",
                    Timestamp = DateTime.Now,
                    Changes = "{}",
                    Action = "Unknown"
                };

                try
                {
                    if (!string.IsNullOrEmpty(CurrentUser.FullName))
                        auditEntry.UserFullName = CurrentUser.FullName;
                }
                catch { }

                var primaryKey = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());
                auditEntry.RecordId = primaryKey?.CurrentValue?.ToString() ?? "New";

                var changesDict = new Dictionary<string, object>();

                // معلومات مختصرة
                if (entry.Entity is OrderDetail od) changesDict["_Info"] = $"Item: {od.ProductName} | Qty: {od.Quantity}";
                else if (entry.Entity is Product p) changesDict["_Info"] = $"Product: {p.Name}";
                else if (entry.Entity is Order o) changesDict["_Info"] = $"Order Total: {o.TotalAmount}";

                // =========================================================
                // (3) تحديد Action + تسجيل التغييرات (Diff فقط)
                // =========================================================

                if (entry.State == EntityState.Added)
                {
                    auditEntry.Action = "Insert";

                    // ✅ ممكن تقيّد إضافة خصائص معينة بدل كل شيء (خصوصًا لو فيه حقول كبيرة)
                    foreach (var prop in entry.Properties)
                    {
                        if (prop.IsTemporary) continue;

                        // تقليل الضوضاء لو أحببت: تجاهل Timestamps
                        if (IsNoiseProperty(prop.Metadata.Name)) continue;

                        changesDict[prop.Metadata.Name] = prop.CurrentValue;
                    }
                }
                else if (entry.State == EntityState.Deleted)
                {
                    auditEntry.Action = "HardDelete";
                    foreach (var prop in entry.Properties)
                    {
                        if (IsNoiseProperty(prop.Metadata.Name)) continue;
                        changesDict[prop.Metadata.Name] = prop.OriginalValue;
                    }
                }
                else if (entry.State == EntityState.Modified)
                {
                    auditEntry.Action = "Update";

                    var isDeletedProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name.Equals("isDeleted", StringComparison.OrdinalIgnoreCase));
                    if (isDeletedProp != null &&
                        (isDeletedProp.CurrentValue as bool? == true) &&
                        (isDeletedProp.OriginalValue as bool? == false))
                    {
                        auditEntry.Action = "SoftDelete";
                    }

                    foreach (var prop in entry.Properties)
                    {
                        if (!prop.IsModified) continue;
                        if (IsNoiseProperty(prop.Metadata.Name)) continue;

                        // ✅ Product: فقط Price/Name
                        if (entry.Entity is Product)
                        {
                            if (!prop.Metadata.Name.Equals("Price", StringComparison.OrdinalIgnoreCase) &&
                                !prop.Metadata.Name.Equals("Name", StringComparison.OrdinalIgnoreCase))
                                continue;
                        }

                        // ✅ Order: فقط حقول مهمة
                        if (entry.Entity is Order)
                        {
                            if (!_orderImportantProps.Contains(prop.Metadata.Name))
                                continue;
                        }

                        // ✅ OrderDetail: فقط حقول مهمة
                        if (entry.Entity is OrderDetail)
                        {
                            if (!_orderDetailImportantProps.Contains(prop.Metadata.Name))
                                continue;
                        }

                        changesDict[prop.Metadata.Name] = new { Old = prop.OriginalValue, New = prop.CurrentValue };
                    }
                }

                // ✅ لا تسجّل إذا ماكو تغييرات مفيدة
                // (_Info وحده لا تكفي — نريد تغيير فعلي)
                if (changesDict.Count <= 1 && changesDict.ContainsKey("_Info"))
                    continue;

                // حفظ السجل
                auditEntry.Changes = JsonSerializer.Serialize(changesDict);
                auditEntries.Add(auditEntry);
            }

            if (auditEntries.Any())
            {
                AuditLogs.AddRange(auditEntries);
            }
        }

        // ✅ فلتر حقول “ضوضاء” عامة (عدّلها حسب مشروعك)
        private static bool IsNoiseProperty(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return true;

            // خصائص تتغير دائمًا أو لا قيمة Audit لها
            return name.Equals("UpdatedAt", StringComparison.OrdinalIgnoreCase)
                || name.Equals("CreatedAt", StringComparison.OrdinalIgnoreCase)
                || name.Equals("RowVersion", StringComparison.OrdinalIgnoreCase)
                || name.Equals("Timestamp", StringComparison.OrdinalIgnoreCase)
                || name.Equals("LastModified", StringComparison.OrdinalIgnoreCase);
        }
    }
}
