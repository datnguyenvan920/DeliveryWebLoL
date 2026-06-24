using DeliveryWebLoL.DTO.Driver;

namespace DeliveryWebLoL.Service.Interfaces
{
    public interface IDriverOrderService
    {
        Task<IReadOnlyList<DriverOrderDto>> GetOrdersForDriverAsync(Guid driverUserId, int take = 200);
        Task<bool> UpdateOrderStatusAsync(Guid driverUserId, Guid orderId, int newStatus);
    }
}
