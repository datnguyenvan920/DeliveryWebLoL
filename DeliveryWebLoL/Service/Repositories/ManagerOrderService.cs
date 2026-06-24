using DeliveryWebLoL.Data;
using DeliveryWebLoL.DTO.Manager;
using DeliveryWebLoL.Models;
using DeliveryWebLoL.Service.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DeliveryWebLoL.Service.Repositories
{
    public class ManagerOrderService : IManagerOrderService
    {
        private readonly ApplicationDbContext _db;

        public ManagerOrderService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IReadOnlyList<ManagerOrderDto>> GetOrdersForOwnedWarehousesAsync(Guid managerUserId, int take = 200)
        {
            if (managerUserId == Guid.Empty) return Array.Empty<ManagerOrderDto>();

            // Manager-owned warehouse ids
            var ownedWarehouseIds = await _db.Locations
                .AsNoTracking()
                .Where(l => l.OwnerUserID == managerUserId && l.LocationType == LocationType.Warehouse)
                .Select(l => l.LocationID)
                .ToListAsync();

            if (ownedWarehouseIds.Count == 0) return Array.Empty<ManagerOrderDto>();

            // Load orders where SourceLocation is one of the owned warehouses
            var orders = await _db.Orders
                .AsNoTracking()
                .Where(o => ownedWarehouseIds.Contains(o.SourceLocationID))
                .OrderByDescending(o => o.CreatedAt)
                .Take(take)
                .Select(o => new ManagerOrderDto
                {
                    OrderId = o.OrderID,
                    CreatedAt = o.CreatedAt,
                    Status = (int)o.Status,
                    OrderType = (int)o.OrderType,
                    SourceLocationId = o.SourceLocationID,
                    SourceLocationName = _db.Locations.Where(l => l.LocationID == o.SourceLocationID).Select(l => l.Name).FirstOrDefault() ?? string.Empty,
                    DestinationLocationId = o.DestinationLocationID,
                    DestinationLocationName = _db.Locations.Where(l => l.LocationID == o.DestinationLocationID).Select(l => l.Name).FirstOrDefault() ?? string.Empty,
                    RequestedByUsername = _db.Users.Where(u => u.UserID == o.RequestedByUserID).Select(u => u.Username).FirstOrDefault() ?? string.Empty,
                    Items = Array.Empty<ManagerOrderLineItemDto>()
                })
                .ToListAsync();

            if (orders.Count == 0) return orders;

            // Fill line items
            var orderIds = orders.Select(o => o.OrderId).ToList();
            var lineItems = await _db.OrderLineItems
                .AsNoTracking()
                .Where(li => orderIds.Contains(li.OrderID))
                .Join(_db.Items.AsNoTracking(), li => li.ItemID, it => it.ItemID, (li, it) => new { li, it })
                .Select(x => new
                {
                    x.li.OrderID,
                    Item = new ManagerOrderLineItemDto
                    {
                        ItemId = x.it.ItemID,
                        SKU = x.it.SKU,
                        Name = x.it.Name,
                        Unit = x.it.Unit,
                        Quantity = x.li.Quantity
                    }
                })
                .ToListAsync();

            var map = lineItems.GroupBy(x => x.OrderID)
                .ToDictionary(g => g.Key, g => (IReadOnlyList<ManagerOrderLineItemDto>)g.Select(x => x.Item).ToList());

            foreach (var o in orders)
            {
                if (map.TryGetValue(o.OrderId, out var items))
                    o.Items = items;
            }

            return orders;
        }
    }
}
