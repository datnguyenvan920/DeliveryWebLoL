using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DeliveryWebLoL.Models
{
    public class Location
    {
        [Key]
        public Guid LocationID { get; set; }

        [Required]
        public Guid OwnerUserID { get; set; }

        [ForeignKey(nameof(OwnerUserID))]
        public User? OwnerUser { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = null!;

        [MaxLength(500)]
        public string? Address { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        [Required]
        public LocationType LocationType { get; set; }

        public ICollection<Inventory>? Inventories { get; set; }
    }
}
