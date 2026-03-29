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
                Role = Enum.IsDefined(typeof(UserRole), role) ? (UserRole)role : UserRole.NewUser,
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
            // Generates a random integer between 0 (inclusive) and 1,000,000 (exclusive)
            int secureNumber = RandomNumberGenerator.GetInt32(0, 1000000);
            // Format the number as a 6-digit string, padding with leading zeros (e.g., 42 becomes "000042")
            return secureNumber.ToString("D6");
        }

        //=================================================================================================================
        public async Task<User?> AuthenticateAsync(string username, string password)
        {
            var user = await _db.Users.SingleOrDefaultAsync(u => u.Username == username);
            if (user == null) return null;

            // Verify the plain password against the stored hash
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
            if (newRole == UserRole.Affiliate && !extraData.IsNullOrEmpty())
            {
                user.AffiliationId = extraData; 
            }
            user.Role = newRole;
            _db.Users.Update(user);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
