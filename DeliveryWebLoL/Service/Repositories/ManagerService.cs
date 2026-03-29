using DeliveryWebLoL.Data;
using DeliveryWebLoL.DTO.Common;
using DeliveryWebLoL.DTO.Manager;
using DeliveryWebLoL.Models;
using DeliveryWebLoL.Service.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DeliveryWebLoL.Service.Repositories
{
    public class ManagerService : IManagerService
    {
        private readonly ApplicationDbContext _db;

        public ManagerService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<PageResponseDto<ManagerWarehouseDto>> GetOwnedWarehousesAsync(Guid managerUserId, ManagerListRequestDto req)
        {
            req ??= new ManagerListRequestDto();

            var query = _db.Locations
                .AsNoTracking()
                .Where(l => l.OwnerUserID == managerUserId && l.LocationType == LocationType.Warehouse);

            if (!string.IsNullOrWhiteSpace(req.Search))
            {
                var s = req.Search.Trim();
                query = query.Where(l => l.Name.Contains(s) || (l.Address != null && l.Address.Contains(s)));
            }

            var total = await query.CountAsync();

            var items = await query
                .OrderBy(l => l.Name)
                .Skip(req.Skip)
                .Take(req.Take)
                .Select(l => new ManagerWarehouseDto
                {
                    LocationID = l.LocationID,
                    Name = l.Name,
                    Address = l.Address,
                    LocationType = (int)l.LocationType
                })
                .ToListAsync();

            return new PageResponseDto<ManagerWarehouseDto>
            {
                Items = items,
                Total = total,
                Page = req.Page,
                PageSize = req.PageSize
            };
        }

        public async Task<PageResponseDto<ManagerDelivererDto>> GetDeliverersForOwnedWarehousesAsync(Guid managerUserId, ManagerListRequestDto req)
        {
            req ??= new ManagerListRequestDto();

            var ownedWarehouseIdsQuery = _db.Locations
                .AsNoTracking()
                .Where(l => l.OwnerUserID == managerUserId && l.LocationType == LocationType.Warehouse)
                .Select(l => l.LocationID);

            if (req.WarehouseLocationId.HasValue)
            {
                ownedWarehouseIdsQuery = ownedWarehouseIdsQuery.Where(id => id == req.WarehouseLocationId.Value);
            }

            var ownedWarehouseIds = await ownedWarehouseIdsQuery.ToListAsync();
            if (ownedWarehouseIds.Count == 0)
            {
                return new PageResponseDto<ManagerDelivererDto>
                {
                    Items = Array.Empty<ManagerDelivererDto>(),
                    Total = 0,
                    Page = req.Page,
                    PageSize = req.PageSize
                };
            }

            var affiliationIds = await _db.AffiliateWarehouses
                .AsNoTracking()
                .Where(aw => ownedWarehouseIds.Contains(aw.WarehouseLocationId))
                .Select(aw => aw.AffiliationId)
                .Distinct()
                .ToListAsync();

            if (affiliationIds.Count == 0)
            {
                return new PageResponseDto<ManagerDelivererDto>
                {
                    Items = Array.Empty<ManagerDelivererDto>(),
                    Total = 0,
                    Page = req.Page,
                    PageSize = req.PageSize
                };
            }

            var affiliationIdStrings = affiliationIds.Select(id => id.ToString()).ToList();

            var query = _db.Users
                .AsNoTracking()
                .Where(u => u.Role == UserRole.Driver && u.AffiliationId != null && affiliationIdStrings.Contains(u.AffiliationId));

            if (!string.IsNullOrWhiteSpace(req.Search))
            {
                var s = req.Search.Trim();
                query = query.Where(u => u.Username.Contains(s) || (u.Email != null && u.Email.Contains(s)) || (u.ContactPhone != null && u.ContactPhone.Contains(s)));
            }

            var total = await query.CountAsync();

            var items = await query
                .OrderBy(u => u.Username)
                .Skip(req.Skip)
                .Take(req.Take)
                .Select(u => new ManagerDelivererDto
                {
                    UserID = u.UserID,
                    Username = u.Username,
                    ContactPhone = u.ContactPhone,
                    Email = u.Email,
                    AffiliationId = u.AffiliationId,
                    Role = (int)u.Role
                })
                .ToListAsync();

            return new PageResponseDto<ManagerDelivererDto>
            {
                Items = items,
                Total = total,
                Page = req.Page,
                PageSize = req.PageSize
            };
        }

        public async Task<ManagerWarehouseDto> AddWarehouseAsync(Guid managerUserId, AddWarehouseRequestDto req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (string.IsNullOrWhiteSpace(req.Name)) throw new ArgumentException("Warehouse name is required.");

            var location = new Location
            {
                LocationID = Guid.NewGuid(),
                OwnerUserID = managerUserId,
                Name = req.Name.Trim(),
                Address = req.Address?.Trim(),
                Latitude = req.Latitude,
                Longitude = req.Longitude,
                LocationType = LocationType.Warehouse
            };

            await _db.Locations.AddAsync(location);
            await _db.SaveChangesAsync();

            return new ManagerWarehouseDto
            {
                LocationID = location.LocationID,
                Name = location.Name,
                Address = location.Address,
                LocationType = (int)location.LocationType
            };
        }

        public async Task<bool> AddDelivererAsync(Guid managerUserId, AddDelivererRequestDto req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.WarehouseLocationId == Guid.Empty) throw new ArgumentException("WarehouseLocationId is required.");
            if (req.DelivererUserId == Guid.Empty) throw new ArgumentException("DelivererUserId is required.");

            // Validate warehouse owned by manager
            var warehouseExists = await _db.Locations
                .AsNoTracking()
                .AnyAsync(l => l.LocationID == req.WarehouseLocationId && l.OwnerUserID == managerUserId && l.LocationType == LocationType.Warehouse);

            if (!warehouseExists) return false;

            // Validate deliverer user exists and is Driver
            var deliverer = await _db.Users.SingleOrDefaultAsync(u => u.UserID == req.DelivererUserId);
            if (deliverer == null) return false;
            if (deliverer.Role != UserRole.Driver) return false;

            // Prevent double-assigning: deliverer already has an affiliation
            if (!string.IsNullOrWhiteSpace(deliverer.AffiliationId)) return false;

            // Create affiliate primary location if provided, otherwise create a placeholder affiliate location
            Guid affiliatePrimaryLocationId;
            if (req.AffiliatePrimaryLocationId.HasValue)
            {
                affiliatePrimaryLocationId = req.AffiliatePrimaryLocationId.Value;

                // Ensure location exists
                var exists = await _db.Locations.AsNoTracking().AnyAsync(l => l.LocationID == affiliatePrimaryLocationId);
                if (!exists) return false;
            }
            else
            {
                // Create a minimal affiliate location owned by this manager for now.
                var affiliateLocation = new Location
                {
                    LocationID = Guid.NewGuid(),
                    OwnerUserID = managerUserId,
                    Name = $"Affiliate-{deliverer.Username}",
                    Address = null,
                    Latitude = null,
                    Longitude = null,
                    LocationType = LocationType.Affiliate
                };

                await _db.Locations.AddAsync(affiliateLocation);
                affiliatePrimaryLocationId = affiliateLocation.LocationID;
            }

            // Create affiliate
            var affiliate = new Affiliate
            {
                LocationId = affiliatePrimaryLocationId,
                CreatedAt = DateTime.UtcNow
            };

            await _db.Affiliates.AddAsync(affiliate);
            await _db.SaveChangesAsync();

            // Link affiliate to selected warehouse (many-to-many join)
            var link = new AffiliateWarehouse
            {
                AffiliationId = affiliate.AffiliationId,
                WarehouseLocationId = req.WarehouseLocationId
            };

            await _db.AffiliateWarehouses.AddAsync(link);

            // Assign deliverer to this affiliate affiliation
            deliverer.AffiliationId = affiliate.AffiliationId.ToString();

            await _db.SaveChangesAsync();

            return true;
        }
    }
}
