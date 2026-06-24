using System;
using System.ComponentModel.DataAnnotations;

namespace DeliveryWebLoL.DTO.Manager
{
    public class ApproveOrderRequestDto
    {
        [Required]
        public Guid OrderId { get; set; }
    }
}
