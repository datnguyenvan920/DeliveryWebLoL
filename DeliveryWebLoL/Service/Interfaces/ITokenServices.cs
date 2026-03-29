using DeliveryWebLoL.Models;

namespace DeliveryWebLoL.Service
{
    public interface ITokenServices
    {
        string CreateAccessToken(User user);
        string CreateRefreshToken();
    }
}