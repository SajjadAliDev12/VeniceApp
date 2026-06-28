using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace VinceApp.Data.Models
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

        // التحكم بتفعيل/تعطيل التسجيل
        private static bool _auditEnabled = true;
        public static void EnableAudit(bool enable) => _auditEnabled = enable;

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

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            configurationBuilder
                .Properties<decimal>()
                .HavePrecision(18, 2);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // =======================
            // Orders
            // =======================
            modelBuilder.Entity<Order>(e =>
            {
                e.Property(x => x.OrderDate).HasColumnType("datetime2");
                e.Property(x => x.TotalAmount).HasPrecision(18, 2);
                e.Property(x => x.DiscountAmount).HasPrecision(18, 2);
                e.Property(x => x.TableId).HasDefaultValue(null);
                e.Property(x => x.isPaid).HasDefaultValue(false);
                e.Property(x => x.isReady).HasDefaultValue(false);
                e.Property(x => x.isServed).HasDefaultValue(false);
                e.Property(x => x.isSentToKitchen).HasDefaultValue(false);
                e.Property(x => x.isDeleted).HasDefaultValue(false);
                e.Property(x => x.isDone).HasDefaultValue(false);


            });

            // =======================
            // OrderDetails
            // =======================
            modelBuilder.Entity<OrderDetail>(e =>
            {
                e.Property(x => x.ProductName).HasMaxLength(200);
                e.Property(x => x.Quantity).HasDefaultValue(1);
                e.Property(x => x.Price).HasPrecision(18, 2);

                e.HasOne(x => x.Order)
                    .WithMany(o => o.OrderDetails)
                    .HasForeignKey(x => x.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.Product)
                    .WithMany(p => p.OrderDetails)
                    .HasForeignKey(x => x.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // =======================
            // Products
            // =======================
            modelBuilder.Entity<Product>(e =>
            {
                e.Property(x => x.Name).HasMaxLength(150).IsRequired();
                e.Property(x => x.Price).HasPrecision(18, 2);
                e.Property(x => x.IsKitchenItem).HasDefaultValue(false);

                e.HasOne(x => x.Category)
                    .WithMany(c => c.Products)
                    .HasForeignKey(x => x.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // =======================
            // Categories
            // =======================
            modelBuilder.Entity<Category>(e =>
            {
                e.Property(x => x.Name).HasMaxLength(100).IsRequired();
                
            });

            // =======================
            // RestaurantTables
            // =======================
            modelBuilder.Entity<RestaurantTable>(e =>
            {
                e.Property(x => x.TableName).HasMaxLength(100);
                e.Property(x => x.Status).HasDefaultValue(0);
                e.HasIndex(x => x.TableNumber).IsUnique();
            });

            // =======================
            // Users
            // =======================
            modelBuilder.Entity<User>(e =>
            {
                e.Property(x => x.Username).HasMaxLength(50).IsRequired();
                e.Property(x => x.EmailAddress).HasMaxLength(150);
                e.Property(x => x.IsEmailConfirmed).HasDefaultValue(false);

                e.HasIndex(x => x.Username).IsUnique();
                e.HasIndex(x => x.EmailAddress).IsUnique();
            });

            // =======================
            // UserTokens
            // =======================
            modelBuilder.Entity<UserToken>(e =>
            {
                e.Property(x => x.Token).HasMaxLength(500).IsRequired();
            });

            // =======================
            // AuditLogs
            // =======================
            modelBuilder.Entity<AuditLog>(e =>
            {
                e.Property(x => x.TableName).HasMaxLength(100);
                e.Property(x => x.UserFullName).HasMaxLength(150);
                e.Property(x => x.Action).HasMaxLength(50);
                e.Property(x => x.RecordId).HasMaxLength(100); // زدنا الطول ليتسع لـ TEMP_123
            });

            // =======================
            // AppSettings
            // =======================
            modelBuilder.Entity<AppSetting>(e =>
            {
                e.Property(x => x.StoreName).HasMaxLength(150);
                e.Property(x => x.StorePhone).HasMaxLength(50);
                e.Property(x => x.PrinterName).HasMaxLength(150);
                e.Property(x => x.SmtpServer).HasMaxLength(150);
                e.Property(x => x.SenderEmail).HasMaxLength(150);
                e.Property(x => x.CloudBackupProvider).HasMaxLength(50);
                e.Property(x => x.CloudUserEmail).HasMaxLength(150);
                e.Property(x => x.CloudRefreshToken).HasMaxLength(2000);
                e.Property(x => x.AutoCloudBackup).HasDefaultValue(false);
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);

        // ---------------------------------------------------------
        //  قوائم الحقول المهمة
        // ---------------------------------------------------------
        private static readonly HashSet<string> _orderImportantProps = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "isSentToKitchen", "isReady", "isServed", "isPaid",
            "isDeleted", "TableId", "TotalAmount", "DiscountAmount", "ParentOrderId"
        };

        private static readonly HashSet<string> _orderDetailImportantProps = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Quantity", "isDeleted", "Price", "ProductId", "ProductName", "OrderId"
        };

        // الحقول التي تتغير باستمرار ولا تهم في التسجيل
        private static readonly HashSet<string> _excludedProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "CreatedAt", "UpdatedAt", "LastModified", "RowVersion",
            "Timestamp", "ConcurrencyToken", "Version", "LastUpdated",
            "ModifiedDate", "CreatedDate"
        };

        // الحقول التفسيرية فقط (ليست مهمة للتسجيل إذا تغيرت وحدها)
        private static readonly HashSet<string> _infoOnlyProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Note", "Comment", "Remarks", "Description", "Notes"
        };

        // ---------------------------------------------------------
        //  SaveChanges Overrides (معدلة للحفظ على مرحلتين لحل الأرقام السالبة)
        // ---------------------------------------------------------
        public override int SaveChanges()
        {
            if (!_auditEnabled) return base.SaveChanges();

            // 1. التقاط اللوجات المؤقتة قبل الحفظ
            var pendingLogs = OnBeforeSaveChanges();

            // 2. الحفظ الفعلي للبيانات (هنا تأخذ الكيانات الجديدة أرقام ID موجبة حقيقية)
            int result = base.SaveChanges();

            // 3. تحديث أرقام اللوجات وحفظها
            OnAfterSaveChanges(pendingLogs);

            return result;
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (!_auditEnabled) return await base.SaveChangesAsync(cancellationToken);

            var pendingLogs = OnBeforeSaveChanges();
            int result = await base.SaveChangesAsync(cancellationToken);
            await OnAfterSaveChangesAsync(pendingLogs);

            return result;
        }

        // ---------------------------------------------------------
        //  الدالة الرئيسية للتحقق قبل الحفظ
        // ---------------------------------------------------------

        // كلاس مساعد لربط الإدخال باللوج لتحديثه لاحقاً
        private class PendingAuditLog
        {
            public EntityEntry Entry { get; set; }
            public AuditLog Log { get; set; }
        }

        // تم تغيير نوع الإرجاع لإرجاع اللوجات المؤقتة
        private List<PendingAuditLog> OnBeforeSaveChanges()
        {
            var pendingLogs = new List<PendingAuditLog>();

            var client = VinceApp.Services.AppConfigService.GetClient();
            bool isKitchenClient = client.Equals("KITCHEN", StringComparison.OrdinalIgnoreCase);

            if (isKitchenClient && !HasImportantKitchenChanges()) return pendingLogs;

            ChangeTracker.DetectChanges();

            int totalChanges = ChangeTracker.Entries()
                .Count(e => e.State != EntityState.Unchanged &&
                           e.State != EntityState.Detached &&
                           !(e.Entity is AuditLog));

            if (totalChanges > 100)
            {
                var warningEntry = new AuditLog
                {
                    TableName = "System",
                    Action = "Warning",
                    Changes = JsonSerializer.Serialize(new
                    {
                        Message = $"Too many changes ({totalChanges}), audit limited to important changes only",
                        Timestamp = DateTime.Now
                    }),
                    Timestamp = DateTime.Now,
                    UserFullName = GetCurrentUserName(),
                    RecordId = "0"
                };
                AuditLogs.Add(warningEntry);
                ProcessLimitedAudit(isKitchenClient, pendingLogs);
                return pendingLogs;
            }

            ProcessFullAudit(isKitchenClient, pendingLogs);
            return pendingLogs;
        }

        private void OnAfterSaveChanges(List<PendingAuditLog> pendingLogs)
        {
            if (pendingLogs == null || pendingLogs.Count == 0) return;

            foreach (var item in pendingLogs)
            {
                // تحديث الـ RecordId للكيانات التي تم إضافتها حديثاً (وأخذت الآن رقم موجب)
                if (item.Entry.State == EntityState.Added)
                {
                    item.Log.RecordId = GetRecordId(item.Entry);
                }
                AuditLogs.Add(item.Log);
            }

            _auditEnabled = false;
            base.SaveChanges();
            _auditEnabled = true;
        }

        private async Task OnAfterSaveChangesAsync(List<PendingAuditLog> pendingLogs)
        {
            if (pendingLogs == null || pendingLogs.Count == 0) return;

            foreach (var item in pendingLogs)
            {
                if (item.Entry.State == EntityState.Added)
                {
                    item.Log.RecordId = GetRecordId(item.Entry);
                }
                AuditLogs.Add(item.Log);
            }

            _auditEnabled = false;
            await base.SaveChangesAsync();
            _auditEnabled = true;
        }

        // ---------------------------------------------------------
        //  دوال المعالجة
        // ---------------------------------------------------------
        private bool HasImportantKitchenChanges()
        {
            return ChangeTracker.Entries<Order>()
                .Any(e => e.State == EntityState.Modified &&
                         (e.Property(p => p.isReady).IsModified ||
                          e.Property(p => p.isServed).IsModified));
        }

        private void ProcessLimitedAudit(bool isKitchenClient, List<PendingAuditLog> pendingLogs)
        {
            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.Entity is AuditLog || entry.State == EntityState.Unchanged || entry.State == EntityState.Detached)
                    continue;

                if (entry.Entity is User || entry.Entity is AppSetting)
                {
                    var auditEntry = BuildAuditEntry(entry, isKitchenClient);
                    if (auditEntry != null) pendingLogs.Add(new PendingAuditLog { Entry = entry, Log = auditEntry });
                }
                else if (entry.Entity is Product &&
                        (entry.State == EntityState.Added || entry.State == EntityState.Deleted))
                {
                    var auditEntry = BuildAuditEntry(entry, isKitchenClient);
                    if (auditEntry != null) pendingLogs.Add(new PendingAuditLog { Entry = entry, Log = auditEntry });
                }
                // تم إزالة تسجيل (Order && EntityState.Added) نهائياً من هنا لعدم تسجيل الطلبات الجديدة
            }
        }

        private void ProcessFullAudit(bool isKitchenClient, List<PendingAuditLog> pendingLogs)
        {
            var softDeletedOrderIds = ChangeTracker.Entries<Order>()
                .Where(e => e.State == EntityState.Modified &&
                           e.Property(p => p.isDeleted).IsModified &&
                           e.Entity.isDeleted)
                .Select(e => e.Entity.Id)
                .ToHashSet();

            var trackedOrders = ChangeTracker.Entries<Order>()
                .ToDictionary(e => e.Entity.Id, e => e.Entity);

            var neededOrderIds = ChangeTracker.Entries<OrderDetail>()
                .Where(e => e.State != EntityState.Unchanged &&
                           e.State != EntityState.Detached)
                .Select(e => e.Entity.OrderId)
                .Distinct()
                .Where(id => !trackedOrders.ContainsKey(id))
                .ToList();

            if (neededOrderIds.Count > 0)
            {
                var ordersFromDb = Orders.AsNoTracking()
                    .Where(o => neededOrderIds.Contains(o.Id))
                    .ToList();

                foreach (var order in ordersFromDb)
                {
                    trackedOrders[order.Id] = order;
                }
            }

            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.Entity is AuditLog || entry.State == EntityState.Unchanged || entry.State == EntityState.Detached)
                    continue;

                if (!ShouldAuditEntry(entry, isKitchenClient, softDeletedOrderIds, trackedOrders))
                    continue;

                var auditEntry = BuildAuditEntry(entry, isKitchenClient);
                if (auditEntry != null) pendingLogs.Add(new PendingAuditLog { Entry = entry, Log = auditEntry });
            }
        }

        // ---------------------------------------------------------
        //  دوال المساعدة
        // ---------------------------------------------------------
        private bool ShouldAuditEntry(EntityEntry entry, bool isKitchenClient,
            HashSet<int> softDeletedOrderIds, Dictionary<int, Order> trackedOrders)
        {
            if (entry.Entity is User || entry.Entity is AppSetting)
                return true;

            if (entry.Entity is Product)
            {
                if (entry.State == EntityState.Added || entry.State == EntityState.Deleted)
                    return true;

                if (entry.State == EntityState.Modified)
                {
                    bool isPriceChanged = entry.Property("Price").IsModified;
                    bool isNameChanged = entry.Property("Name").IsModified;
                    return isPriceChanged || isNameChanged;
                }
                return false;
            }

            if (entry.Entity is Order order)
            {
                // ✅ التعديل هنا: منع تسجيل إنشاء طلب جديد (الطلبات الفارغة)
                if (entry.State == EntityState.Added)
                    return false;

                if (entry.State == EntityState.Deleted)
                    return false;

                if (entry.State == EntityState.Modified)
                {
                    if (isKitchenClient)
                    {
                        return entry.Properties.Any(p =>
                            p.IsModified &&
                            (p.Metadata.Name.Equals("isReady", StringComparison.OrdinalIgnoreCase) ||
                             p.Metadata.Name.Equals("isServed", StringComparison.OrdinalIgnoreCase)));
                    }
                    else
                    {
                        return entry.Properties.Any(p =>
                            p.IsModified && _orderImportantProps.Contains(p.Metadata.Name));
                    }
                }
            }

            if (entry.Entity is OrderDetail detail)
            {
                if (isKitchenClient) return false;

                if (softDeletedOrderIds.Contains(detail.OrderId)) return false;

                bool isSentToKitchen = false;
                if (trackedOrders.TryGetValue(detail.OrderId, out var or))
                {
                    isSentToKitchen = or.isSentToKitchen;
                }
                else
                {
                    var orderFromDb = Orders.AsNoTracking()
                        .FirstOrDefault(o => o.Id == detail.OrderId);
                    isSentToKitchen = orderFromDb?.isSentToKitchen ?? false;
                }

                if (!isSentToKitchen) return false;

                if (entry.State == EntityState.Added || entry.State == EntityState.Deleted)
                    return true;

                if (entry.State == EntityState.Modified)
                {
                    var importantChanges = entry.Properties
                        .Count(p => p.IsModified &&
                                   _orderDetailImportantProps.Contains(p.Metadata.Name) &&
                                   !_infoOnlyProperties.Contains(p.Metadata.Name));

                    var allChanges = entry.Properties.Where(p => p.IsModified).ToList();
                    var onlyInfoChanges = allChanges.All(p =>
                        _infoOnlyProperties.Contains(p.Metadata.Name) ||
                        _excludedProperties.Contains(p.Metadata.Name));

                    return importantChanges > 0 && !onlyInfoChanges;
                }
            }

            return false;
        }

        private AuditLog BuildAuditEntry(EntityEntry entry, bool isKitchenClient)
        {
            string tableName = entry.Entity.GetType().Name;
            string userName = GetCurrentUserName();

            // ✅ يتم وضع كلمة مؤقتة، وسيتم استبدالها بالرقم الموجب لاحقاً
            string recordId = entry.State == EntityState.Added ? "Pending" : GetRecordId(entry);

            string action = GetAction(entry);
            var changesDict = BuildChangesDictionary(entry, tableName);

            if (changesDict.Count <= 1 && changesDict.ContainsKey("_Info"))
                return null;

            AddAdditionalInfo(entry, changesDict);

            return new AuditLog
            {
                TableName = tableName,
                UserFullName = userName,
                Timestamp = DateTime.Now,
                Changes = JsonSerializer.Serialize(changesDict),
                Action = action,
                RecordId = recordId
            };
        }

        private string GetCurrentUserName()
        {
            try
            {
                return string.IsNullOrEmpty(CurrentUser.FullName) ? "System" : CurrentUser.FullName;
            }
            catch
            {
                return "System";
            }
        }

        private string GetRecordId(EntityEntry entry)
        {
            var primaryKey = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());
            return primaryKey?.CurrentValue?.ToString() ?? "Unknown";
        }

        private string GetAction(EntityEntry entry)
        {
            return entry.State switch
            {
                EntityState.Added => "Insert",
                EntityState.Deleted => "HardDelete",
                EntityState.Modified when IsSoftDelete(entry) => "SoftDelete",
                EntityState.Modified => "Update",
                _ => "Unknown"
            };
        }

        private bool IsSoftDelete(EntityEntry entry)
        {
            var isDeletedProp = entry.Properties
                .FirstOrDefault(p => p.Metadata.Name.Equals("isDeleted", StringComparison.OrdinalIgnoreCase));

            return isDeletedProp != null &&
                   (isDeletedProp.CurrentValue as bool? == true) &&
                   (isDeletedProp.OriginalValue as bool? == false);
        }

        private Dictionary<string, object> BuildChangesDictionary(EntityEntry entry, string tableName)
        {
            var changesDict = new Dictionary<string, object>();

            if (entry.State == EntityState.Added)
            {
                foreach (var prop in entry.Properties)
                {
                    if (prop.IsTemporary || IsNoiseProperty(prop.Metadata.Name)) continue;

                    if (entry.Entity is Product &&
                        !prop.Metadata.Name.Equals("Name", StringComparison.OrdinalIgnoreCase) &&
                        !prop.Metadata.Name.Equals("Price", StringComparison.OrdinalIgnoreCase))
                        continue;

                    changesDict[prop.Metadata.Name] = prop.CurrentValue;
                }
            }
            else if (entry.State == EntityState.Deleted)
            {
                foreach (var prop in entry.Properties)
                {
                    if (IsNoiseProperty(prop.Metadata.Name)) continue;
                    changesDict[prop.Metadata.Name] = prop.OriginalValue;
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                foreach (var prop in entry.Properties)
                {
                    if (!prop.IsModified || IsNoiseProperty(prop.Metadata.Name)) continue;

                    if (entry.Entity is Product)
                    {
                        if (!prop.Metadata.Name.Equals("Price", StringComparison.OrdinalIgnoreCase) &&
                            !prop.Metadata.Name.Equals("Name", StringComparison.OrdinalIgnoreCase))
                            continue;
                    }
                    else if (entry.Entity is Order)
                    {
                        if (!_orderImportantProps.Contains(prop.Metadata.Name))
                            continue;
                    }
                    else if (entry.Entity is OrderDetail)
                    {
                        if (!_orderDetailImportantProps.Contains(prop.Metadata.Name) ||
                            _infoOnlyProperties.Contains(prop.Metadata.Name))
                            continue;
                    }

                    changesDict[prop.Metadata.Name] = new
                    {
                        Old = prop.OriginalValue,
                        New = prop.CurrentValue
                    };
                }
            }

            return changesDict;
        }

        private void AddAdditionalInfo(EntityEntry entry, Dictionary<string, object> changesDict)
        {
            if (entry.Entity is OrderDetail od)
                changesDict["_Info"] = $"{od.ProductName} (Qty: {od.Quantity})";
            else if (entry.Entity is Product p)
                changesDict["_Info"] = p.Name;
            else if (entry.Entity is Order o)
                changesDict["_Info"] = $"Order #{o.Id} - Total: {o.TotalAmount}";
        }

        private static bool IsNoiseProperty(string name)
        {
            return string.IsNullOrWhiteSpace(name) ||
                   _excludedProperties.Contains(name);
        }
    }
}