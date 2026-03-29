using System.ComponentModel.DataAnnotations;

namespace DeliveryWebLoL.Models
{
    public class OrderLineItem
    {
        [Key]
        public Guid LineItemID { get; set; }

        [Required]
        public Guid OrderID { get; set; }
        public Order? Order { get; set; }

        [Required]
        public Guid ItemID { get; set; }
        public Item? Item { get; set; }

        public decimal Quantity { get; set; }
    }
}
