using System.ComponentModel.DataAnnotations;

namespace DeliveryWebLoL.Models
{
    public class Order
    {
        [Key]
        public Guid OrderID { get; set; }

        [Required]
        public Guid RequestedByUserID { get; set; }
        public User? RequestedByUser { get; set; }

        public Guid? ApprovedByUserID { get; set; }
        public User? ApprovedByUser { get; set; }

        [Required]
        public Guid SourceLocationID { get; set; }
        public Location? SourceLocation { get; set; }

        [Required]
        public Guid DestinationLocationID { get; set; }
        public Location? DestinationLocation { get; set; }

        [Required]
        public OrderType OrderType { get; set; }

        [Required]
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<OrderLineItem>? OrderLineItems { get; set; }
    }
}
