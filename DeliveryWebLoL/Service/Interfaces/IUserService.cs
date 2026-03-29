using DeliveryWebLoL.Models;

namespace DeliveryWebLoL.Service.Interfaces
{
    public interface IUserService
    {
        Task<User?> AuthenticateAsync(string username, string password);
        Task<User?> GetByUsernameAsync(string username);
        Task<User?> GetByIdAsync(Guid id);
        Task UpdateRefreshTokenAsync(User user, string refreshToken, DateTime expiry);
        Task<User?> RegisterAsync(string username, string password, string phonenumber, string? email, int role);
        Task<bool> UpdateRoleAsync(User user, UserRole newRole, string extraData);
    }
}
