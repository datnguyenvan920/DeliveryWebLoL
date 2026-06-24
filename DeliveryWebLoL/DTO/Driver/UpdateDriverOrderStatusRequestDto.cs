using System;
using System.ComponentModel.DataAnnotations;

namespace DeliveryWebLoL.DTO.Driver
{
    public class UpdateDriverOrderStatusRequestDto
    {
        [Required]
        public Guid OrderId { get; set; }

        [Required]
        public int NewStatus { get; set; }
    }
}
