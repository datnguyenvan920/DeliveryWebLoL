using DeliveryWebLoL.Data;
using DeliveryWebLoL.DTO.Manager;
using DeliveryWebLoL.Models;
using DeliveryWebLoL.Service.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DeliveryWebLoL.Service.Repositories
{
    public class WarehouseInventoryService : IWarehouseInventoryService
    {
        private readonly ApplicationDbContext _db;

        public WarehouseInventoryService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IReadOnlyList<WarehouseItemDto>> GetWarehouseItemsAsync(Guid managerUserId, Guid warehouseLocationId)
        {
            if (warehouseLocationId == Guid.Empty)
                throw new ArgumentException("warehouseLocationId is required.");

            // Validate ownership
            var isOwned = await _db.Locations.AsNoTracking().AnyAsync(l =>
                l.LocationID == warehouseLocationId &&
                l.OwnerUserID == managerUserId &&
                l.LocationType == LocationType.Warehouse);

            if (!isOwned)
                throw new InvalidOperationException("Warehouse not found or not owned by manager.");

            // Left join Inventory + Item + Production rule (if any)
            var rows = await _db.Inventories
                .AsNoTracking()
                .Where(i => i.LocationID == warehouseLocationId)
                .Join(_db.Items.AsNoTracking(), i => i.ItemID, it => it.ItemID, (i, it) => new { i, it })
                .GroupJoin(
                    _db.LocationItemProductions.AsNoTracking().Where(p => p.LocationID == warehouseLocationId),
                    x => x.it.ItemID,
                    p => p.ItemID,
                    (x, prods) => new { x.i, x.it, prod = prods.FirstOrDefault() })
                .Select(x => new WarehouseItemDto
                {
                    ItemId = x.it.ItemID,
                    SKU = x.it.SKU,
                    Name = x.it.Name,
                    ItemCategory = (int)x.it.ItemCategory,
                    Unit = x.it.Unit,
                    Quantity = x.i.Quantity,
                    UnitsPerMinute = x.prod != null ? x.prod.UnitsPerMinute : 0,
                    IsProductionEnabled = x.prod != null && x.prod.IsEnabled,
                    LastUpdated = x.i.LastUpdated
                })
                .OrderBy(x => x.Name)
                .ToListAsync();

            return rows;
        }
    }
}
