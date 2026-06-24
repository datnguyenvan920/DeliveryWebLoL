using DeliveryWebLoL.Data;
using DeliveryWebLoL.Models;
using DeliveryWebLoL.Service.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;

namespace DeliveryWebLoL.Service.Repositories
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _db;
        private readonly PasswordHasher<User> _hasher;

        public UserService(ApplicationDbContext db)
        {
            _db = db;
            _hasher = new PasswordHasher<User>();
        }

        public async Task<User?> RegisterAsync(string username, string password, string phonenumber, string? email, int role)
        {
            // Ensure username, phone and email (when provided) are unique
            if (await _db.Users.AnyAsync(u =>
                u.Username == username ||
                (u.ContactPhone != null && u.ContactPhone == phonenumber) ||
                (email != null && u.Email == email)))
            {
                return null;
            }

            var user = new User
            {
                UserID = Guid.NewGuid(),
                Username = username,
                ContactPhone = phonenumber,
                Email = email,
                Role = Enum.IsDefined(typeof(UserRole), role) ? (UserRole)role : UserRole.Affiliate,
                IsActive = true,
                VerifyNumber = GenerateSixDigitOTP(),
            };

            user.PasswordHash = _hasher.HashPassword(user, password);

            await _db.Users.AddAsync(user);
            await _db.SaveChangesAsync();

            return user;
        }

        public static string GenerateSixDigitOTP()
        {
            int secureNumber = RandomNumberGenerator.GetInt32(0, 1000000);
            return secureNumber.ToString("D6");
        }

        public async Task<User?> AuthenticateAsync(string username, string password)
        {
            var user = await _db.Users.SingleOrDefaultAsync(u => u.Username == username);
            if (user == null) return null;

            var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
            return result == PasswordVerificationResult.Success ? user : null;
        }

        public Task<User?> GetByUsernameAsync(string username)
        {
            return _db.Users.SingleOrDefaultAsync(u => u.Username == username);
        }

        public async Task<User?> GetByIdAsync(Guid id)
        {
            return await _db.Users.FindAsync(id);
        }

        public async Task UpdateRefreshTokenAsync(User user, string refreshToken, DateTime expiry)
        {
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = expiry;
            _db.Users.Update(user);
            await _db.SaveChangesAsync();
        }

        public async Task<bool> UpdateRoleAsync(User user, UserRole newRole, string extraData)
        {
            if (user == null) return false;

            var prevRole = user.Role;
            user.Role = newRole;

            // If the user is becoming an Affiliate and has no affiliation/destination yet,
            // create a personal affiliate Location and store its GUID in user.AffiliationId.
            // This LocationID will be used as Order.DestinationLocationID.
            if (newRole == UserRole.Affiliate && prevRole == UserRole.NewUser && string.IsNullOrWhiteSpace(user.AffiliationId))
            {
                var loc = new Location
                {
                    LocationID = Guid.NewGuid(),
                    OwnerUserID = user.UserID,
                    Name = $"Affiliate-{user.Username}",
                    Address = null,
                    Latitude = null,
                    Longitude = null,
                    LocationType = LocationType.Affiliate
                };

                await _db.Locations.AddAsync(loc);
                user.AffiliationId = loc.LocationID.ToString();
            }

            // extraData semantics:
            // - If extraData is GUID: interpret as WarehouseLocationId and map to AffiliateWarehouse.AffiliationId.
            // - Else (non-GUID): IGNORE (do not overwrite existing AffiliationId).
            if (!extraData.IsNullOrEmpty())
            {
                var trimmed = extraData.Trim();

                if (Guid.TryParse(trimmed, out var warehouseLocationId) && warehouseLocationId != Guid.Empty)
                {
                    var affiliationId = await _db.AffiliateWarehouses
                        .AsNoTracking()
                        .Where(aw => aw.WarehouseLocationId == warehouseLocationId)
                        .Select(aw => (int?)aw.AffiliationId)
                        .FirstOrDefaultAsync();

                    if (!affiliationId.HasValue)
                        return false;

                    user.AffiliationId = affiliationId.Value.ToString();
                }
            }

            _db.Users.Update(user);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task LogoutAsync(User user)
        {
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
            _db.Users.Update(user);
            await _db.SaveChangesAsync();
        }

        public async Task<bool> ClaimAffiliateAffiliationAsync(Guid userId, Guid affiliateLocationCode)
        {
            if (userId == Guid.Empty || affiliateLocationCode == Guid.Empty) return false;

            var user = await _db.Users.SingleOrDefaultAsync(u => u.UserID == userId);
            if (user == null) return false;

            // Only allow the recovery if there's no affiliation yet.
            if (!string.IsNullOrWhiteSpace(user.AffiliationId)) return false;

            // In this flow, the "code" is the WarehouseLocationId.
            var affId = await _db.AffiliateWarehouses
                .AsNoTracking()
                .Where(aw => aw.WarehouseLocationId == affiliateLocationCode)
                .Select(aw => (int?)aw.AffiliationId)
                .FirstOrDefaultAsync();

            if (!affId.HasValue) return false;

            user.AffiliationId = affId.Value.ToString();
            _db.Users.Update(user);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
