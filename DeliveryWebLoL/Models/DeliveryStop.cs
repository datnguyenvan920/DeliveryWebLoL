using System.ComponentModel.DataAnnotations;

namespace DeliveryWebLoL.Models
{
    public class DeliveryStop
    {
        [Key]
        public Guid StopID { get; set; }

        [Required]
        public Guid DeliveryID { get; set; }
        public Delivery? Delivery { get; set; }

        [Required]
        public Guid OrderID { get; set; }
        public Order? Order { get; set; }

        public int SequenceOrder { get; set; }

        public DateTime EstimatedArrivalTime { get; set; }

        public DateTime? ActualArrivalTime { get; set; }

        public StopStatus Status { get; set; } = StopStatus.Pending;
    }
}
