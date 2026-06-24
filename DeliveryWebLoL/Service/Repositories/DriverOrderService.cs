using DeliveryWebLoL.Data;
using DeliveryWebLoL.DTO.Driver;
using DeliveryWebLoL.Models;
using DeliveryWebLoL.Service.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DeliveryWebLoL.Service.Repositories
{
    public class DriverOrderService : IDriverOrderService
    {
        private readonly ApplicationDbContext _db;

        public DriverOrderService(ApplicationDbContext db)
        {
            _db = db;
        }

        private async Task<Guid?> ResolveDriverWarehouseAsync(Guid driverUserId)
        {
            // Driver/User.AffiliationId stores numeric Affiliate.AffiliationId
            var affStr = await _db.Users
                .AsNoTracking()
                .Where(u => u.UserID == driverUserId)
                .Select(u => u.AffiliationId)
                .FirstOrDefaultAsync();

            if (string.IsNullOrWhiteSpace(affStr) || !int.TryParse(affStr, out var affiliationId))
                return null;

            var whId = await _db.AffiliateWarehouses
                .AsNoTracking()
                .Where(aw => aw.AffiliationId == affiliationId)
                .Select(aw => (Guid?)aw.WarehouseLocationId)
                .FirstOrDefaultAsync();

            return whId;
        }

        public async Task<IReadOnlyList<DriverOrderDto>> GetOrdersForDriverAsync(Guid driverUserId, int take = 200)
        {
            var whId = await ResolveDriverWarehouseAsync(driverUserId);
            if (!whId.HasValue) return Array.Empty<DriverOrderDto>();

            var orders = await _db.Orders
                .AsNoTracking()
                .Where(o => o.SourceLocationID == whId.Value)
                .OrderByDescending(o => o.CreatedAt)
                .Take(take)
                .Select(o => new DriverOrderDto
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
                    Items = Array.Empty<DriverOrderLineItemDto>()
                })
                .ToListAsync();

            if (orders.Count == 0) return orders;

            var orderIds = orders.Select(o => o.OrderId).ToList();
            var lineItems = await _db.OrderLineItems
                .AsNoTracking()
                .Where(li => orderIds.Contains(li.OrderID))
                .Join(_db.Items.AsNoTracking(), li => li.ItemID, it => it.ItemID, (li, it) => new { li, it })
                .Select(x => new { x.li.OrderID, Item = new DriverOrderLineItemDto { ItemId = x.it.ItemID, SKU = x.it.SKU, Name = x.it.Name, Unit = x.it.Unit, Quantity = x.li.Quantity } })
                .ToListAsync();

            var map = lineItems.GroupBy(x => x.OrderID)
                .ToDictionary(g => g.Key, g => (IReadOnlyList<DriverOrderLineItemDto>)g.Select(x => x.Item).ToList());

            foreach (var o in orders)
            {
                if (map.TryGetValue(o.OrderId, out var items))
                    o.Items = items;
            }

            return orders;
        }

        public async Task<bool> UpdateOrderStatusAsync(Guid driverUserId, Guid orderId, int newStatus)
        {
            if (driverUserId == Guid.Empty || orderId == Guid.Empty) return false;

            var whId = await ResolveDriverWarehouseAsync(driverUserId);
            if (!whId.HasValue) return false;

            var order = await _db.Orders.SingleOrDefaultAsync(o => o.OrderID == orderId);
            if (order == null) return false;

            // Driver can only act on orders from their warehouse
            if (order.SourceLocationID != whId.Value) return false;

            if (!Enum.IsDefined(typeof(OrderStatus), newStatus)) return false;
            var ns = (OrderStatus)newStatus;

            // Allowed transitions:
            // ReadyForPickup (3) -> InTransit (4)
            // InTransit (4) -> Delivered (5)
            if (order.Status == OrderStatus.ReadyForPickup && ns == OrderStatus.InTransit)
            {
                order.Status = ns;
            }
            else if (order.Status == OrderStatus.InTransit && ns == OrderStatus.Delivered)
            {
                order.Status = ns;
            }
            else
            {
                return false;
            }

            _db.Orders.Update(order);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
