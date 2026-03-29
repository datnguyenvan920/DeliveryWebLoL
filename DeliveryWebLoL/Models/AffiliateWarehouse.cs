using System.ComponentModel.DataAnnotations.Schema;

namespace DeliveryWebLoL.Models
{
    // Join table to represent many-to-many between Affiliates and warehouse Locations
    public class AffiliateWarehouse
    {
        public int AffiliationId { get; set; }
        public Affiliate? Affiliate { get; set; }

        public Guid WarehouseLocationId { get; set; }

        [ForeignKey(nameof(WarehouseLocationId))]
        public Location? WarehouseLocation { get; set; }
    }
}
