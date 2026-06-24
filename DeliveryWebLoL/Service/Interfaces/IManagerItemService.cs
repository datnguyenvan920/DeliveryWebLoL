using DeliveryWebLoL.DTO.Manager;

namespace DeliveryWebLoL.Service.Interfaces
{
    public interface IManagerItemService
    {
        Task<CreateWarehouseItemResponseDto> CreateWarehouseItemAsync(Guid managerUserId, CreateWarehouseItemRequestDto req);
    }
}
