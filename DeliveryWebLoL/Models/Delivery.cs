using System.ComponentModel.DataAnnotations;

namespace DeliveryWebLoL.Models
{
    public class Delivery
    {
        [Key]
        public Guid DeliveryID { get; set; }

        public Guid? DriverUserID { get; set; }
        public User? DriverUser { get; set; }

        public string? VehicleInfo { get; set; }

        public DeliveryStatus Status { get; set; } = DeliveryStatus.Assigned;

        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        public ICollection<DeliveryStop>? DeliveryStops { get; set; }
    }
}
