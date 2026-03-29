namespace DeliveryWebLoL.DTO.Manager
{
    public class AddDelivererRequestDto
    {
        // Location (warehouse) owned by the manager to associate the deliverer with
        public Guid WarehouseLocationId { get; set; }

        // Deliverer user account to assign. This user should have Role == Driver.
        public Guid DelivererUserId { get; set; }

        // Optional: affiliate primary location. If not provided, can be created later.
        public Guid? AffiliatePrimaryLocationId { get; set; }
    }
}
