using System;

namespace DeliveryWebLoL.DTO.Affiliate
{
    public class AffiliateContextDto
    {
        public Guid WarehouseLocationId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public Guid? DestinationLocationId { get; set; }
    }
}
