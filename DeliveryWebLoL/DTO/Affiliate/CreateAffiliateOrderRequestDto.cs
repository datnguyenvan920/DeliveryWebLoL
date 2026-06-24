using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DeliveryWebLoL.DTO.Affiliate
{
    public class CreateAffiliateOrderRequestDto
    {
        [Required]
        public Guid SourceWarehouseLocationId { get; set; }

        [Required]
        public Guid DestinationLocationId { get; set; }

        [Required]
        public int OrderType { get; set; }

        [Required]
        [MinLength(1)]
        public List<CreateAffiliateOrderLineItemDto> Items { get; set; } = new();
    }

    public class CreateAffiliateOrderLineItemDto
    {
        [Required]
        public Guid ItemId { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal Quantity { get; set; }
    }
}
