namespace DeliveryWebLoL.DTO.Manager
{
    public class AddDelivererRequestDto
    {
        // Location (warehouse) owned by the manager to associate the deliverer with
        public Guid WarehouseLocationId { get; set; }

        // OPTIONAL: Deliverer user account to assign.
        // If omitted, the API will only create the Affiliate and link it to the warehouse.
        public Guid? DelivererUserId { get; set; }

        // Optional: affiliate primary location. If not provided, a placeholder location will be created.
        public Guid? AffiliatePrimaryLocationId { get; set; }
    }
}
