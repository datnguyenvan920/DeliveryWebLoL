using DeliveryWebLoL.DTO.Common;

namespace DeliveryWebLoL.DTO.Manager
{
    public class ManagerHomeResponseDto
    {
        public required PageResponseDto<ManagerWarehouseDto> Warehouses { get; init; }
        public required PageResponseDto<ManagerDelivererDto> Deliverers { get; init; }
    }
}
