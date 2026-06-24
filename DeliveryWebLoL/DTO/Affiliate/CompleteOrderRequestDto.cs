using System;
using System.ComponentModel.DataAnnotations;

namespace DeliveryWebLoL.DTO.Affiliate
{
    public class CompleteOrderRequestDto
    {
        [Required]
        public Guid OrderId { get; set; }
    }
}
