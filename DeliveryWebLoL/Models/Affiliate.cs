using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DeliveryWebLoL.Models
{
    public class Affiliate
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int AffiliationId { get; set; }

        // The primary location representing the affiliate (required)
        [Required]
        public Guid LocationId { get; set; }

        [ForeignKey(nameof(LocationId))]
        public Location? Location { get; set; }

        // Additional field (CreatedAt) — you can replace with Name/Code if you prefer
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Many-to-many link to warehouse locations (see AffiliateWarehouse)
        public ICollection<AffiliateWarehouse>? WarehouseLinks { get; set; }
    }
}
