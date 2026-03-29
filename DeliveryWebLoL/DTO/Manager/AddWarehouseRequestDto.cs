namespace DeliveryWebLoL.DTO.Manager
{
    public class AddWarehouseRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}
