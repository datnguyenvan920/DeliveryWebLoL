using DeliveryWebLoL.Data;
using DeliveryWebLoL.DTO.Manager;
using DeliveryWebLoL.Models;
using DeliveryWebLoL.Service.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DeliveryWebLoL.Service.Repositories
{
    public class OrderApprovalService : IOrderApprovalService
    {
        private readonly ApplicationDbContext _db;

        public OrderApprovalService(ApplicationDbContext db)
        {
            _db = db;
        }

        private async Task<bool> IsOrderFromOwnedWarehouseAsync(Guid managerUserId, Guid orderId)
        {
            return await _db.Orders
                .AsNoTracking()
                .AnyAsync(o => o.OrderID == orderId && _db.Locations.Any(l => l.LocationID == o.SourceLocationID && l.OwnerUserID == managerUserId && l.LocationType == LocationType.Warehouse));
        }

        public async Task<ManagerOrderDto?> GetOrderDetailForManagerAsync(Guid managerUserId, Guid orderId)
        {
            if (managerUserId == Guid.Empty || orderId == Guid.Empty) return null;
            if (!await IsOrderFromOwnedWarehouseAsync(managerUserId, orderId)) return null;

            var dto = await _db.Orders
                .AsNoTracking()
                .Where(o => o.OrderID == orderId)
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
                .FirstOrDefaultAsync();

            if (dto == null) return null;

            var items = await _db.OrderLineItems
                .AsNoTracking()
                .Where(li => li.OrderID == orderId)
                .Join(_db.Items.AsNoTracking(), li => li.ItemID, it => it.ItemID, (li, it) => new ManagerOrderLineItemDto
                {
                    ItemId = it.ItemID,
                    SKU = it.SKU,
                    Name = it.Name,
                    Unit = it.Unit,
                    Quantity = li.Quantity
                })
                .OrderBy(x => x.Name)
                .ToListAsync();

            dto.Items = items;
            return dto;
        }

        public async Task<bool> ApproveOrderAsync(Guid managerUserId, Guid orderId)
        {
            if (managerUserId == Guid.Empty || orderId == Guid.Empty) return false;
            if (!await IsOrderFromOwnedWarehouseAsync(managerUserId, orderId)) return false;

            var order = await _db.Orders.SingleOrDefaultAsync(o => o.OrderID == orderId);
            if (order == null) return false;

            // Only approve Pending orders
            if (order.Status != OrderStatus.Pending) return false;

            // Per requirement: after manager approves, order becomes ReadyForPickup.
            order.ApprovedByUserID = managerUserId;
            order.Status = OrderStatus.ReadyForPickup;

            _db.Orders.Update(order);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
