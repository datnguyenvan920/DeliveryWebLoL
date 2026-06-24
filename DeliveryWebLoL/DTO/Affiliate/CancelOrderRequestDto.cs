using System;
using System.ComponentModel.DataAnnotations;

namespace DeliveryWebLoL.DTO.Affiliate
{
    public class CancelOrderRequestDto
    {
        [Required]
        public Guid OrderId { get; set; }
    }
}
