using System;

namespace DeliveryWebLoL.DTO.Affiliate
{
    public class AffiliateWarehouseItemDto
    {
        public Guid ItemId { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Unit { get; set; }
        public decimal Quantity { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
