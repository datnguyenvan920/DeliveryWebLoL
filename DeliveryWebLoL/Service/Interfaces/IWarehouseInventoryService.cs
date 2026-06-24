using DeliveryWebLoL.DTO.Manager;

namespace DeliveryWebLoL.Service.Interfaces
{
    public interface IWarehouseInventoryService
    {
        Task<IReadOnlyList<WarehouseItemDto>> GetWarehouseItemsAsync(Guid managerUserId, Guid warehouseLocationId);
    }
}
