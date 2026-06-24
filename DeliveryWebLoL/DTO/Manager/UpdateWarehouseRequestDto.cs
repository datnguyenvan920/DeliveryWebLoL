using System;
using System.ComponentModel.DataAnnotations;

namespace DeliveryWebLoL.DTO.Manager
{
    public class UpdateWarehouseRequestDto
    {
        [Required]
        public Guid WarehouseLocationId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Address { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}
