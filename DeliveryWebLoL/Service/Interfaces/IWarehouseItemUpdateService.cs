using DeliveryWebLoL.DTO.Manager;

namespace DeliveryWebLoL.Service.Interfaces
{
    public interface IWarehouseItemUpdateService
    {
        Task<bool> UpdateWarehouseItemAsync(Guid managerUserId, UpdateWarehouseItemRequestDto req);
    }
}
