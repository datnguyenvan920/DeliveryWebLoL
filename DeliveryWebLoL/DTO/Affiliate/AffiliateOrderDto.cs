using System;
using System.Collections.Generic;

namespace DeliveryWebLoL.DTO.Affiliate
{
    public class AffiliateOrderDto
    {
        public Guid OrderId { get; set; }
        public DateTime CreatedAt { get; set; }
        public int Status { get; set; }
        public int OrderType { get; set; }
        public Guid SourceLocationId { get; set; }
        public Guid DestinationLocationId { get; set; }
        public IReadOnlyList<AffiliateOrderLineItemDto> Items { get; set; } = Array.Empty<AffiliateOrderLineItemDto>();
    }

    public class AffiliateOrderLineItemDto
    {
        public Guid ItemId { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Unit { get; set; }
        public decimal Quantity { get; set; }
    }
}
