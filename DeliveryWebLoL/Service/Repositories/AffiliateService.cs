using DeliveryWebLoL.Data;
using DeliveryWebLoL.DTO.Affiliate;
using DeliveryWebLoL.Models;
using DeliveryWebLoL.Service.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DeliveryWebLoL.Service.Repositories
{
    public class AffiliateService : IAffiliateService
    {
        private readonly ApplicationDbContext _db;

        public AffiliateService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<Guid?> ResolveMyWarehouseAsync(Guid affiliateUserId)
        {
            // User.AffiliationId can be either:
            // - numeric affiliationId (legacy / warehouse-based linking)
            // - GUID of the affiliate's own destination location (new sign-up flow)
            var affiliationIdStr = await _db.Users
                .AsNoTracking()
                .Where(u => u.UserID == affiliateUserId)
                .Select(u => u.AffiliationId)
                .FirstOrDefaultAsync();

            if (string.IsNullOrWhiteSpace(affiliationIdStr)) return null;

            if (!int.TryParse(affiliationIdStr, out var affiliationId))
            {
                // Not numeric => destination location id stored here; warehouse link must be derived elsewhere.
                return null;
            }

            var warehouseLocationId = await _db.AffiliateWarehouses
                .AsNoTracking()
                .Where(aw => aw.AffiliationId == affiliationId)
                .Select(aw => (Guid?)aw.WarehouseLocationId)
                .FirstOrDefaultAsync();

            return warehouseLocationId;
        }

        public async Task<AffiliateContextDto?> GetMyContextAsync(Guid affiliateUserId)
        {
            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserID == affiliateUserId);
            if (user == null) return null;

            // Destination location: prefer an Affiliate-type location owned by this user
            var destinationLocationId = await _db.Locations
                .AsNoTracking()
                .Where(l => l.OwnerUserID == affiliateUserId && l.LocationType == LocationType.Affiliate)
                .Select(l => (Guid?)l.LocationID)
                .FirstOrDefaultAsync();

            // Fallback: any location owned by user
            if (!destinationLocationId.HasValue)
            {
                destinationLocationId = await _db.Locations
                    .AsNoTracking()
                    .Where(l => l.OwnerUserID == affiliateUserId)
                    .Select(l => (Guid?)l.LocationID)
                    .FirstOrDefaultAsync();
            }

            var warehouseLocationId = await ResolveMyWarehouseAsync(affiliateUserId);

            var warehouseName = string.Empty;
            if (warehouseLocationId.HasValue)
            {
                warehouseName = await _db.Locations
                    .AsNoTracking()
                    .Where(l => l.LocationID == warehouseLocationId.Value)
                    .Select(l => l.Name)
                    .FirstOrDefaultAsync() ?? string.Empty;
            }

            return new AffiliateContextDto
            {
                WarehouseLocationId = warehouseLocationId ?? Guid.Empty,
                WarehouseName = warehouseName,
                DestinationLocationId = destinationLocationId
            };
        }

        public async Task<IReadOnlyList<AffiliateWarehouseItemDto>> GetMyWarehouseItemsAsync(Guid affiliateUserId)
        {
            var warehouseLocationId = await ResolveMyWarehouseAsync(affiliateUserId);
            if (!warehouseLocationId.HasValue) return Array.Empty<AffiliateWarehouseItemDto>();

            // Inventory for that warehouse
            var rows = await _db.Inventories
                .AsNoTracking()
                .Where(i => i.LocationID == warehouseLocationId.Value)
                .Join(_db.Items.AsNoTracking(), i => i.ItemID, it => it.ItemID, (inv, it) => new { inv, it })
                .OrderBy(x => x.it.Name)
                .Select(x => new AffiliateWarehouseItemDto
                {
                    ItemId = x.it.ItemID,
                    SKU = x.it.SKU,
                    Name = x.it.Name,
                    Unit = x.it.Unit,
                    Quantity = x.inv.Quantity,
                    LastUpdated = x.inv.LastUpdated
                })
                .ToListAsync();

            return rows;
        }

        public async Task<IReadOnlyList<AffiliateOrderDto>> GetMyOrdersAsync(Guid affiliateUserId)
        {
            var orders = await _db.Orders
                .AsNoTracking()
                .Where(o => o.RequestedByUserID == affiliateUserId)
                .OrderByDescending(o => o.CreatedAt)
                .Take(100)
                .Select(o => new AffiliateOrderDto
                {
                    OrderId = o.OrderID,
                    CreatedAt = o.CreatedAt,
                    Status = (int)o.Status,
                    OrderType = (int)o.OrderType,
                    SourceLocationId = o.SourceLocationID,
                    DestinationLocationId = o.DestinationLocationID,
                    Items = Array.Empty<AffiliateOrderLineItemDto>()
                })
                .ToListAsync();

            // Fill items (simple approach)
            var orderIds = orders.Select(o => o.OrderId).ToList();
            var lineItems = await _db.OrderLineItems
                .AsNoTracking()
                .Where(li => orderIds.Contains(li.OrderID))
                .Join(_db.Items.AsNoTracking(), li => li.ItemID, it => it.ItemID, (li, it) => new { li, it })
                .Select(x => new
                {
                    x.li.OrderID,
                    Item = new AffiliateOrderLineItemDto
                    {
                        ItemId = x.it.ItemID,
                        SKU = x.it.SKU,
                        Name = x.it.Name,
                        Unit = x.it.Unit,
                        Quantity = x.li.Quantity
                    }
                })
                .ToListAsync();

            var map = lineItems.GroupBy(x => x.OrderID).ToDictionary(g => g.Key, g => (IReadOnlyList<AffiliateOrderLineItemDto>)g.Select(x => x.Item).ToList());
            foreach (var o in orders)
            {
                if (map.TryGetValue(o.OrderId, out var items))
                    o.Items = items;
            }

            return orders;
        }

        public async Task<AffiliateOrderDto> CreateOrderAsync(Guid affiliateUserId, CreateAffiliateOrderRequestDto req)
        {
            if (req.SourceWarehouseLocationId == Guid.Empty) throw new ArgumentException("SourceWarehouseLocationId is required");
            if (req.DestinationLocationId == Guid.Empty) throw new ArgumentException("DestinationLocationId is required");
            if (req.Items == null || req.Items.Count == 0) throw new ArgumentException("At least one item is required");

            // Verify affiliate is allowed to order from this warehouse.
            var affiliationIdStr = await _db.Users
                .AsNoTracking()
                .Where(u => u.UserID == affiliateUserId)
                .Select(u => u.AffiliationId)
                .FirstOrDefaultAsync();

            if (string.IsNullOrWhiteSpace(affiliationIdStr) || !int.TryParse(affiliationIdStr, out var affiliationId))
                throw new InvalidOperationException("Affiliate has no affiliation id.");

            var allowed = await _db.AffiliateWarehouses
                .AsNoTracking()
                .AnyAsync(aw => aw.AffiliationId == affiliationId && aw.WarehouseLocationId == req.SourceWarehouseLocationId);

            if (!allowed) throw new InvalidOperationException("You are not linked to this warehouse.");

            // Validate destination location exists
            var destExists = await _db.Locations.AsNoTracking().AnyAsync(l => l.LocationID == req.DestinationLocationId);
            if (!destExists) throw new InvalidOperationException("Destination location not found.");

            // Validate inventory sufficiency
            var itemIds = req.Items.Select(i => i.ItemId).Distinct().ToList();
            var inv = await _db.Inventories
                .Where(i => i.LocationID == req.SourceWarehouseLocationId && itemIds.Contains(i.ItemID))
                .ToListAsync();

            foreach (var li in req.Items)
            {
                var row = inv.FirstOrDefault(x => x.ItemID == li.ItemId);
                if (row == null) throw new InvalidOperationException("Item not found in warehouse inventory.");
                if (li.Quantity <= 0) throw new InvalidOperationException("Quantity must be > 0.");
                if (row.Quantity < li.Quantity) throw new InvalidOperationException($"Not enough stock for item {li.ItemId}.");
            }

            using var tx = await _db.Database.BeginTransactionAsync();

            var order = new Order
            {
                OrderID = Guid.NewGuid(),
                RequestedByUserID = affiliateUserId,
                ApprovedByUserID = null,
                SourceLocationID = req.SourceWarehouseLocationId,
                DestinationLocationID = req.DestinationLocationId,
                OrderType = Enum.IsDefined(typeof(OrderType), req.OrderType) ? (OrderType)req.OrderType : OrderType.Import_Ingredient,
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _db.Orders.AddAsync(order);

            // Deduct inventory and create lineitems
            foreach (var li in req.Items)
            {
                var invRow = inv.First(x => x.ItemID == li.ItemId);
                invRow.Quantity -= li.Quantity;
                invRow.LastUpdated = DateTime.UtcNow;

                await _db.OrderLineItems.AddAsync(new OrderLineItem
                {
                    LineItemID = Guid.NewGuid(),
                    OrderID = order.OrderID,
                    ItemID = li.ItemId,
                    Quantity = li.Quantity
                });
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            // Return created order DTO
            var created = (await GetMyOrdersAsync(affiliateUserId)).First(o => o.OrderId == order.OrderID);
            return created;
        }

        public async Task<bool> CompleteOrderAsync(Guid affiliateUserId, Guid orderId)
        {
            if (affiliateUserId == Guid.Empty || orderId == Guid.Empty) return false;

            var ctx = await GetMyContextAsync(affiliateUserId);
            if (ctx?.DestinationLocationId == null || ctx.DestinationLocationId == Guid.Empty) return false;

            var order = await _db.Orders.SingleOrDefaultAsync(o => o.OrderID == orderId);
            if (order == null) return false;

            // Must be delivered to this affiliate destination
            if (order.DestinationLocationID != ctx.DestinationLocationId.Value) return false;

            if (order.Status != OrderStatus.Delivered) return false;

            order.Status = OrderStatus.Completed;
            _db.Orders.Update(order);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CancelOrderAsync(Guid affiliateUserId, Guid orderId)
        {
            if (affiliateUserId == Guid.Empty || orderId == Guid.Empty) return false;

            var order = await _db.Orders.SingleOrDefaultAsync(o => o.OrderID == orderId);
            if (order == null) return false;

            // Only the requesting affiliate can cancel
            if (order.RequestedByUserID != affiliateUserId) return false;

            // Only allow cancel while still Pending (inventory was deducted at creation)
            if (order.Status != OrderStatus.Pending) return false;

            // Load line items
            var lineItems = await _db.OrderLineItems
                .Where(li => li.OrderID == orderId)
                .ToListAsync();

            if (lineItems.Count == 0)
            {
                order.Status = OrderStatus.Cancelled;
                _db.Orders.Update(order);
                await _db.SaveChangesAsync();
                return true;
            }

            var itemIds = lineItems.Select(li => li.ItemID).Distinct().ToList();

            using var tx = await _db.Database.BeginTransactionAsync();

            // Restore inventory at the source warehouse
            var invRows = await _db.Inventories
                .Where(i => i.LocationID == order.SourceLocationID && itemIds.Contains(i.ItemID))
                .ToListAsync();

            foreach (var li in lineItems)
            {
                var inv = invRows.FirstOrDefault(r => r.ItemID == li.ItemID);
                if (inv == null)
                {
                    // Inventory row missing; create it so stock is not lost
                    inv = new Inventory
                    {
                        InventoryID = Guid.NewGuid(),
                        LocationID = order.SourceLocationID,
                        ItemID = li.ItemID,
                        Quantity = 0m,
                        LastUpdated = DateTime.UtcNow
                    };
                    await _db.Inventories.AddAsync(inv);
                    invRows.Add(inv);
                }

                inv.Quantity += li.Quantity;
                inv.LastUpdated = DateTime.UtcNow;
            }

            order.Status = OrderStatus.Cancelled;
            _db.Orders.Update(order);

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return true;
        }
    }
}
