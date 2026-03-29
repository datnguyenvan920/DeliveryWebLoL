using DeliveryWebLoL.Data;
using DeliveryWebLoL.Models;
using DeliveryWebLoL.Service.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DeliveryWebLoL.Service.Repositories
{
    public class ProductionService : IProductionService
    {
        private readonly ApplicationDbContext _db;

        public ProductionService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task ApplyProductionAsync(Guid locationId, Guid itemId, DateTime? nowUtc = null)
        {
            var now = nowUtc ?? DateTime.UtcNow;

            var rule = await _db.LocationItemProductions
                .SingleOrDefaultAsync(p => p.LocationID == locationId && p.ItemID == itemId);

            if (rule == null || !rule.IsEnabled)
            {
                return;
            }

            // If somehow LastCalculatedAt is in the future, reset.
            if (rule.LastCalculatedAt > now)
            {
                rule.LastCalculatedAt = now;
                await _db.SaveChangesAsync();
                return;
            }

            var elapsedMinutes = (decimal)(now - rule.LastCalculatedAt).TotalMinutes;
            if (elapsedMinutes <= 0)
            {
                return;
            }

            var produced = elapsedMinutes * rule.UnitsPerMinute;
            if (produced <= 0)
            {
                rule.LastCalculatedAt = now;
                await _db.SaveChangesAsync();
                return;
            }

            var inventory = await _db.Inventories
                .SingleOrDefaultAsync(i => i.LocationID == locationId && i.ItemID == itemId);

            if (inventory == null)
            {
                inventory = new Inventory
                {
                    InventoryID = Guid.NewGuid(),
                    LocationID = locationId,
                    ItemID = itemId,
                    Quantity = 0,
                    LastUpdated = now
                };
                await _db.Inventories.AddAsync(inventory);
            }

            inventory.Quantity += produced;
            inventory.LastUpdated = now;

            rule.LastCalculatedAt = now;

            await _db.SaveChangesAsync();
        }

        public async Task UpdateProductionRateAsync(Guid locationId, Guid itemId, decimal unitsPerMinute, bool isEnabled, DateTime? nowUtc = null)
        {
            var now = nowUtc ?? DateTime.UtcNow;

            // catch up with the existing rule (if any) before changing the rate
            await ApplyProductionAsync(locationId, itemId, now);

            var rule = await _db.LocationItemProductions
                .SingleOrDefaultAsync(p => p.LocationID == locationId && p.ItemID == itemId);

            if (rule == null)
            {
                rule = new LocationItemProduction
                {
                    LocationID = locationId,
                    ItemID = itemId,
                    UnitsPerMinute = unitsPerMinute,
                    IsEnabled = isEnabled,
                    LastCalculatedAt = now,
                };

                await _db.LocationItemProductions.AddAsync(rule);
            }
            else
            {
                rule.UnitsPerMinute = unitsPerMinute;
                rule.IsEnabled = isEnabled;
                rule.LastCalculatedAt = now;
            }

            await _db.SaveChangesAsync();
        }
    }
}
