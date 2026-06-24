using System.ComponentModel.DataAnnotations;

namespace DeliveryWebLoL.DTO.Manager
{
    public class UpdateWarehouseItemRequestDto
    {
        [Required]
        public Guid WarehouseLocationId { get; set; }

        [Required]
        public Guid ItemId { get; set; }

        // Item master data
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = null!;

        [MaxLength(50)]
        public string? Unit { get; set; }

        // Production rule updates
        [Range(typeof(decimal), "0", "79228162514264337593543950335")]
        public decimal UnitsPerMinute { get; set; }

        public bool IsEnabled { get; set; }
    }
}
