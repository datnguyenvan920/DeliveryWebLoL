using System.ComponentModel.DataAnnotations;

namespace DeliveryWebLoL.Models
{
    public class Item
    {
        [Key]
        public Guid ItemID { get; set; }

        [Required]
        [MaxLength(100)]
        public string SKU { get; set; } = null!;

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = null!;

        [Required]
        public ItemCategory ItemCategory { get; set; }

        [MaxLength(50)]
        public string? Unit { get; set; }

        public ICollection<Inventory>? Inventories { get; set; }
        public ICollection<OrderLineItem>? OrderLineItems { get; set; }
    }
}
