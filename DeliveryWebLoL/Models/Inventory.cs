using System.ComponentModel.DataAnnotations;

namespace DeliveryWebLoL.Models
{
    public class Inventory
    {
        [Key]
        public Guid InventoryID { get; set; }

        [Required]
        public Guid LocationID { get; set; }
        public Location? Location { get; set; }

        [Required]
        public Guid ItemID { get; set; }
        public Item? Item { get; set; }

        public decimal Quantity { get; set; }

        public DateTime LastUpdated { get; set; }
    }
}
