namespace DeliveryWebLoL.DTO.Manager
{
    public class ManagerDelivererDto
    {
        public Guid UserID { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? ContactPhone { get; set; }
        public string? Email { get; set; }
        public string? AffiliationId { get; set; }
        public int Role { get; set; }
    }
}
