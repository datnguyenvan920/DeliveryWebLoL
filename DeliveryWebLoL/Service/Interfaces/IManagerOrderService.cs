using DeliveryWebLoL.DTO.Manager;

namespace DeliveryWebLoL.Service.Interfaces
{
    public interface IManagerOrderService
    {
        Task<IReadOnlyList<ManagerOrderDto>> GetOrdersForOwnedWarehousesAsync(Guid managerUserId, int take = 200);
    }
}
