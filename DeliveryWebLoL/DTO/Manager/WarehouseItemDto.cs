namespace DeliveryWebLoL.DTO.Manager
{
    public class WarehouseItemDto
    {
        public Guid ItemId { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int ItemCategory { get; set; }
        public string? Unit { get; set; }

        public decimal Quantity { get; set; }
        public decimal UnitsPerMinute { get; set; }
        public bool IsProductionEnabled { get; set; }
        public DateTime? LastUpdated { get; set; }
    }
}
