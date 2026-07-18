using Microsoft.EntityFrameworkCore;
using EFastCommerce.Core.Entities;
using EFastCommerce.Core.Interfaces;

namespace EFastCommerce.Infrastructure.Data
{
    public class EFastCommerceContext : DbContext
    {
        private readonly ITenantProvider _tenantProvider;

        public EFastCommerceContext(DbContextOptions<EFastCommerceContext> options, ITenantProvider tenantProvider)
            : base(options)
        {
            _tenantProvider = tenantProvider;
        }

        public DbSet<Tenant> Tenants => Set<Tenant>();
        public DbSet<User> Users => Set<User>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();
        public DbSet<StoreInvitation> StoreInvitations => Set<StoreInvitation>();
        public DbSet<StoreSubscription> StoreSubscriptions => Set<StoreSubscription>();
        public DbSet<TenantVendor> TenantVendors => Set<TenantVendor>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Tenant
            modelBuilder.Entity<Tenant>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Slug).IsUnique();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(150);
                entity.Property(e => e.Slug).IsRequired().HasMaxLength(50);

                entity.HasOne(e => e.Owner)
                      .WithMany(u => u.OwnedTenants)
                      .HasForeignKey(e => e.OwnerId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure User
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(150);
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.Property(e => e.Role).IsRequired().HasMaxLength(50);
            });

            // Configure Product
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasQueryFilter(p => _tenantProvider.TenantId == null || p.TenantId == _tenantProvider.TenantId);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(150);
                entity.Property(e => e.Price).HasPrecision(18, 2);
                entity.Property(e => e.MeasurementUnit)
                      .HasConversion<string>()
                      .HasMaxLength(20)
                      .HasDefaultValue(MeasurementUnit.Piece);
                entity.Property(e => e.Size)
                      .HasMaxLength(50)
                      .IsRequired(false);

                entity.HasOne(e => e.Tenant)
                      .WithMany()
                      .HasForeignKey(e => e.TenantId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Order
            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasQueryFilter(o => _tenantProvider.TenantId == null || o.TenantId == _tenantProvider.TenantId);
                entity.Property(e => e.CustomerName).IsRequired().HasMaxLength(150);
                entity.Property(e => e.CustomerEmail).IsRequired().HasMaxLength(150);
                entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);

                entity.HasOne(e => e.Tenant)
                      .WithMany()
                      .HasForeignKey(e => e.TenantId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // Configure OrderItem
            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Price).HasPrecision(18, 2);

                entity.HasOne(e => e.Order)
                      .WithMany(o => o.OrderItems)
                      .HasForeignKey(e => e.OrderId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Product)
                      .WithMany()
                      .HasForeignKey(e => e.ProductId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure StoreInvitation
            modelBuilder.Entity<StoreInvitation>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasQueryFilter(i => _tenantProvider.TenantId == null || i.TenantId == _tenantProvider.TenantId);
                entity.Property(e => e.Token).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.Token).IsUnique();

                entity.HasOne(e => e.Tenant)
                      .WithMany()
                      .HasForeignKey(e => e.TenantId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.ReferrerUser)
                      .WithMany()
                      .HasForeignKey(e => e.ReferrerUserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure StoreSubscription
            modelBuilder.Entity<StoreSubscription>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasQueryFilter(s => _tenantProvider.TenantId == null || s.TenantId == _tenantProvider.TenantId);
                entity.HasIndex(e => new { e.TenantId, e.UserId }).IsUnique();

                entity.HasOne(e => e.Tenant)
                      .WithMany()
                      .HasForeignKey(e => e.TenantId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.User)
                      .WithMany(u => u.StoreSubscriptions)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.ReferredByUser)
                      .WithMany()
                      .HasForeignKey(e => e.ReferredByUserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });



            // Configure TenantVendor
            modelBuilder.Entity<TenantVendor>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasQueryFilter(v => _tenantProvider.TenantId == null || v.TenantId == _tenantProvider.TenantId);
                entity.HasIndex(e => new { e.TenantId, e.UserId }).IsUnique();

                entity.HasOne(e => e.Tenant)
                      .WithMany(t => t.Vendors)
                      .HasForeignKey(e => e.TenantId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.User)
                      .WithMany(u => u.TenantVendors)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
