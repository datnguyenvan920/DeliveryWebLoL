namespace DeliveryWebLoL.DTO.Manager
{
    public class ManagerWarehouseDto
    {
        public Guid LocationID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public int LocationType { get; set; }
    }
}
