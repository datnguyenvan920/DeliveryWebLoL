using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DeliveryWebLoL.Models
{
    // Production rule for a given item at a given location.
    // Stored separately from Inventory to avoid bloating the Inventory table.
    public class LocationItemProduction
    {
        [Required]
        public Guid LocationID { get; set; }

        [ForeignKey(nameof(LocationID))]
        public Location? Location { get; set; }

        [Required]
        public Guid ItemID { get; set; }

        [ForeignKey(nameof(ItemID))]
        public Item? Item { get; set; }

        // Units produced per minute. Example: 0.1 = 1 unit / 10 minutes.
        [Required]
        public decimal UnitsPerMinute { get; set; }

        public bool IsEnabled { get; set; } = true;

        // Last time production was applied to Inventory.
        public DateTime LastCalculatedAt { get; set; } = DateTime.UtcNow;
    }
}
