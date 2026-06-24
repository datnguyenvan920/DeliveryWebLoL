using System.ComponentModel.DataAnnotations;

namespace DeliveryWebLoL.DTO.Manager
{
    public class CreateWarehouseItemRequestDto
    {
        [Required]
        public Guid WarehouseLocationId { get; set; }

        // Item info
        [Required]
        [MaxLength(100)]
        public string SKU { get; set; } = null!;

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = null!;

        [Required]
        public int ItemCategory { get; set; }

        [MaxLength(50)]
        public string? Unit { get; set; }

        // Production rule
        [Range(typeof(decimal), "0", "79228162514264337593543950335")]
        public decimal UnitsPerMinute { get; set; } = 0;

        public bool IsProductionEnabled { get; set; } = true;

        // Inventory init
        [Range(typeof(decimal), "0", "79228162514264337593543950335")]
        public decimal InitialQuantity { get; set; } = 0;
    }
}
