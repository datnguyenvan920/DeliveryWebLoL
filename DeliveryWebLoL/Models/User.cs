using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DeliveryWebLoL.Models
{
    public class User
    {
        [Key]
        public Guid UserID { get; set; }

        [Required]
        [MaxLength(256)]
        public string Username { get; set; } = null!;

        [Required]
        public string PasswordHash { get; set; } = null!;

        [Required]
        public UserRole Role { get; set; }   

        [MaxLength(50)]
        public string? ContactPhone { get; set; }

        [MaxLength(256)]
        [EmailAddress]
        public string? Email { get; set; }

        // New: optional affiliation id (not a foreign key)
        public string? AffiliationId { get; set; }

        public bool IsActive { get; set; } = true;

        // Refresh token for JWT refresh flow
        [MaxLength(512)]
        public string? RefreshToken { get; set; }

        // When the refresh token expires
        public DateTime? RefreshTokenExpiryTime { get; set; }

        // Email/phone verification flag
        public string VerifyNumber { get; set; } 

        // When the verification token/expires (nullable)
        public DateTime? VerifyExpiration { get; set; }

        // Navigation
        public ICollection<Location>? Locations { get; set; }
        public ICollection<Order>? RequestedOrders { get; set; }
        public ICollection<Order>? ApprovedOrders { get; set; }
        public ICollection<Delivery>? Deliveries { get; set; }
    }
}
