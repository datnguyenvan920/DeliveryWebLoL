using DeliveryWebLoL.DTO.Common;

namespace DeliveryWebLoL.DTO.Manager
{
    public class ManagerListRequestDto : PageRequestDto
    {
        // Optional filter: warehouse id (only return deliverers associated with this warehouse)
        public Guid? WarehouseLocationId { get; set; }
    }
}
