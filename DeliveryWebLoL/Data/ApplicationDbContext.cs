using Microsoft.EntityFrameworkCore;
using DeliveryWebLoL.Models;

namespace DeliveryWebLoL.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Location> Locations { get; set; } = null!;
        public DbSet<Item> Items { get; set; } = null!;
        public DbSet<Inventory> Inventories { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<OrderLineItem> OrderLineItems { get; set; } = null!;
        public DbSet<Delivery> Deliveries { get; set; } = null!;
        public DbSet<DeliveryStop> DeliveryStops { get; set; } = null!;
        public DbSet<Affiliate> Affiliates { get; set; } = null!;
        public DbSet<AffiliateWarehouse> AffiliateWarehouses { get; set; } = null!;
        public DbSet<LocationItemProduction> LocationItemProductions { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(b =>
            {
                b.HasIndex(u => u.Username).IsUnique();
                b.HasIndex(u => u.Email).IsUnique().HasFilter("[Email] IS NOT NULL");
                b.HasMany(u => u.Locations).WithOne(l => l.OwnerUser).HasForeignKey(l => l.OwnerUserID).OnDelete(DeleteBehavior.Cascade);
                b.HasMany(u => u.RequestedOrders).WithOne(o => o.RequestedByUser).HasForeignKey(o => o.RequestedByUserID).OnDelete(DeleteBehavior.Restrict);
                b.HasMany(u => u.ApprovedOrders).WithOne(o => o.ApprovedByUser).HasForeignKey(o => o.ApprovedByUserID).OnDelete(DeleteBehavior.Restrict);
                b.HasMany(u => u.Deliveries).WithOne(d => d.DriverUser).HasForeignKey(d => d.DriverUserID).OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Location>(b =>
            {
                b.HasMany(l => l.Inventories).WithOne(i => i.Location).HasForeignKey(i => i.LocationID);
            });

            modelBuilder.Entity<Inventory>(b =>
            {
                // Ensure one inventory row per (LocationID, ItemID)
                b.HasIndex(i => new { i.LocationID, i.ItemID }).IsUnique();
            });

            modelBuilder.Entity<LocationItemProduction>(b =>
            {
                // Composite key: one production rule per (LocationID, ItemID)
                b.HasKey(p => new { p.LocationID, p.ItemID });

                // Precision for UnitsPerMinute
                b.Property(p => p.UnitsPerMinute).HasColumnType("decimal(18,6)");

                b.HasOne(p => p.Location)
                    .WithMany()
                    .HasForeignKey(p => p.LocationID)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasOne(p => p.Item)
                    .WithMany()
                    .HasForeignKey(p => p.ItemID)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasIndex(p => p.LocationID);
                b.HasIndex(p => p.ItemID);
            });

            modelBuilder.Entity<Affiliate>(b =>
            {
                b.HasKey(a => a.AffiliationId);
                b.HasOne(a => a.Location).WithMany().HasForeignKey(a => a.LocationId).OnDelete(DeleteBehavior.Restrict);
                b.HasMany(a => a.WarehouseLinks).WithOne(aw => aw.Affiliate).HasForeignKey(aw => aw.AffiliationId);
            });

            modelBuilder.Entity<AffiliateWarehouse>(b =>
            {
                b.HasKey(aw => new { aw.AffiliationId, aw.WarehouseLocationId });
                b.HasOne(aw => aw.WarehouseLocation).WithMany().HasForeignKey(aw => aw.WarehouseLocationId).OnDelete(DeleteBehavior.Restrict);
                b.HasOne(aw => aw.Affiliate).WithMany(a => a.WarehouseLinks).HasForeignKey(aw => aw.AffiliationId);
            });

            modelBuilder.Entity<Item>(b =>
            {
                b.HasIndex(i => i.SKU).IsUnique();
                b.HasMany(i => i.Inventories).WithOne(inv => inv.Item).HasForeignKey(inv => inv.ItemID);
                b.HasMany(i => i.OrderLineItems).WithOne(li => li.Item).HasForeignKey(li => li.ItemID);
            });

            modelBuilder.Entity<Order>(b =>
            {
                b.HasMany(o => o.OrderLineItems).WithOne(li => li.Order).HasForeignKey(li => li.OrderID);
                b.HasOne(o => o.SourceLocation).WithMany().HasForeignKey(o => o.SourceLocationID).OnDelete(DeleteBehavior.Restrict);
                b.HasOne(o => o.DestinationLocation).WithMany().HasForeignKey(o => o.DestinationLocationID).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Delivery>(b =>
            {
                b.HasMany(d => d.DeliveryStops).WithOne(ds => ds.Delivery).HasForeignKey(ds => ds.DeliveryID);
            });

            modelBuilder.Entity<DeliveryStop>(b =>
            {
                b.HasOne(ds => ds.Order).WithMany().HasForeignKey(ds => ds.OrderID).OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
