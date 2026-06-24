using System;
using System.Collections.Generic;

namespace DeliveryWebLoL.DTO.Driver
{
    public class DriverOrderDto
    {
        public Guid OrderId { get; set; }
        public DateTime CreatedAt { get; set; }
        public int Status { get; set; }
        public int OrderType { get; set; }
        public Guid SourceLocationId { get; set; }
        public string SourceLocationName { get; set; } = string.Empty;
        public Guid DestinationLocationId { get; set; }
        public string DestinationLocationName { get; set; } = string.Empty;
        public string RequestedByUsername { get; set; } = string.Empty;
        public IReadOnlyList<DriverOrderLineItemDto> Items { get; set; } = Array.Empty<DriverOrderLineItemDto>();
    }

    public class DriverOrderLineItemDto
    {
        public Guid ItemId { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Unit { get; set; }
        public decimal Quantity { get; set; }
    }
}
