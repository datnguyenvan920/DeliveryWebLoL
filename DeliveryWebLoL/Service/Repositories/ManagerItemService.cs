using DeliveryWebLoL.Data;
using DeliveryWebLoL.DTO.Manager;
using DeliveryWebLoL.Models;
using DeliveryWebLoL.Service.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DeliveryWebLoL.Service.Repositories
{
    public class ManagerItemService : IManagerItemService
    {
        private readonly ApplicationDbContext _db;

        public ManagerItemService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<CreateWarehouseItemResponseDto> CreateWarehouseItemAsync(Guid managerUserId, CreateWarehouseItemRequestDto req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.WarehouseLocationId == Guid.Empty) throw new ArgumentException("WarehouseLocationId is required.");
            if (string.IsNullOrWhiteSpace(req.SKU)) throw new ArgumentException("SKU is required.");
            if (string.IsNullOrWhiteSpace(req.Name)) throw new ArgumentException("Name is required.");
            if (!Enum.IsDefined(typeof(ItemCategory), req.ItemCategory)) throw new ArgumentException("Invalid ItemCategory.");
            if (req.UnitsPerMinute < 0) throw new ArgumentException("UnitsPerMinute must be >= 0.");
            if (req.InitialQuantity < 0) throw new ArgumentException("InitialQuantity must be >= 0.");

            var now = DateTime.UtcNow;

            // Validate warehouse ownership
            var warehouse = await _db.Locations
                .SingleOrDefaultAsync(l => l.LocationID == req.WarehouseLocationId 
                    && l.OwnerUserID == managerUserId 
                    && l.LocationType == LocationType.Warehouse);

            if (warehouse == null)
                throw new InvalidOperationException("Warehouse not found or not owned by manager.");

            // Enforce global SKU uniqueness (matches EF index on Item.SKU)
            var trimmedSku = req.SKU.Trim();
            var skuExists = await _db.Items.AsNoTracking().AnyAsync(i => i.SKU == trimmedSku);
            if (skuExists)
                throw new InvalidOperationException("SKU already exists.");

            // Ensure this warehouse doesn't already have inventory/production entry with that SKU
            // (defensive, since inventory is keyed by ItemID)

            using var tx = await _db.Database.BeginTransactionAsync();

            var item = new Item
            {
                ItemID = Guid.NewGuid(),
                SKU = trimmedSku,
                Name = req.Name.Trim(),
                Unit = string.IsNullOrWhiteSpace(req.Unit) ? null : req.Unit.Trim(),
                ItemCategory = (ItemCategory)req.ItemCategory
            };
            await _db.Items.AddAsync(item);

            var inventory = new Inventory
            {
                InventoryID = Guid.NewGuid(),
                LocationID = warehouse.LocationID,
                ItemID = item.ItemID,
                Quantity = req.InitialQuantity,
                LastUpdated = now
            };
            await _db.Inventories.AddAsync(inventory);

            var production = new LocationItemProduction
            {
                LocationID = warehouse.LocationID,
                ItemID = item.ItemID,
                UnitsPerMinute = req.UnitsPerMinute,
                IsEnabled = req.IsProductionEnabled,
                LastCalculatedAt = now
            };
            await _db.LocationItemProductions.AddAsync(production);

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return new CreateWarehouseItemResponseDto
            {
                ItemId = item.ItemID,
                WarehouseLocationId = warehouse.LocationID,
                SKU = item.SKU,
                Name = item.Name,
                ItemCategory = (int)item.ItemCategory,
                Unit = item.Unit,
                InitialQuantity = inventory.Quantity,
                UnitsPerMinute = production.UnitsPerMinute,
                IsProductionEnabled = production.IsEnabled
            };
        }
    }
}
