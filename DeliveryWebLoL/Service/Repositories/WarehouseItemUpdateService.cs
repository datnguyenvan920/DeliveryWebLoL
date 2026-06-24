using DeliveryWebLoL.Data;
using DeliveryWebLoL.DTO.Manager;
using DeliveryWebLoL.Models;
using Microsoft.EntityFrameworkCore;

namespace DeliveryWebLoL.Service.Repositories
{
    public class WarehouseItemUpdateService : DeliveryWebLoL.Service.Interfaces.IWarehouseItemUpdateService
    {
        private readonly ApplicationDbContext _db;

        public WarehouseItemUpdateService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<bool> UpdateWarehouseItemAsync(Guid managerUserId, UpdateWarehouseItemRequestDto req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.WarehouseLocationId == Guid.Empty) throw new ArgumentException("WarehouseLocationId is required.");
            if (req.ItemId == Guid.Empty) throw new ArgumentException("ItemId is required.");
            if (string.IsNullOrWhiteSpace(req.Name)) throw new ArgumentException("Name is required.");
            if (req.UnitsPerMinute < 0) throw new ArgumentException("UnitsPerMinute must be >= 0.");

            var now = DateTime.UtcNow;

            // Validate warehouse ownership
            var isOwned = await _db.Locations.AsNoTracking().AnyAsync(l =>
                l.LocationID == req.WarehouseLocationId &&
                l.OwnerUserID == managerUserId &&
                l.LocationType == LocationType.Warehouse);

            if (!isOwned) return false;

            using var tx = await _db.Database.BeginTransactionAsync();

            // Ensure item exists
            var item = await _db.Items.SingleOrDefaultAsync(i => i.ItemID == req.ItemId);
            if (item == null) return false;

            // Ensure inventory exists for that warehouse & item (means it belongs to that warehouse)
            var inventory = await _db.Inventories.SingleOrDefaultAsync(i => i.LocationID == req.WarehouseLocationId && i.ItemID == req.ItemId);
            if (inventory == null) return false;

            // Update item master data
            item.Name = req.Name.Trim();
            item.Unit = string.IsNullOrWhiteSpace(req.Unit) ? null : req.Unit.Trim();

            // Update or create production rule
            var rule = await _db.LocationItemProductions
                .SingleOrDefaultAsync(p => p.LocationID == req.WarehouseLocationId && p.ItemID == req.ItemId);

            if (rule == null)
            {
                rule = new LocationItemProduction
                {
                    LocationID = req.WarehouseLocationId,
                    ItemID = req.ItemId,
                    UnitsPerMinute = req.UnitsPerMinute,
                    IsEnabled = req.IsEnabled,
                    LastCalculatedAt = now
                };
                await _db.LocationItemProductions.AddAsync(rule);
            }
            else
            {
                // If we are turning production ON from OFF, reset LastCalculatedAt to now
                // to prevent "ghost" production for the disabled period.
                var turningOn = !rule.IsEnabled && req.IsEnabled;

                rule.UnitsPerMinute = req.UnitsPerMinute;
                rule.IsEnabled = req.IsEnabled;

                if (turningOn)
                {
                    rule.LastCalculatedAt = now;
                }
                // If staying enabled/disabled, keep LastCalculatedAt as-is
                // so ApplyProduction continues correctly.
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return true;
        }
    }
}
