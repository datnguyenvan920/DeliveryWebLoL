namespace DeliveryWebLoL.DTO.Manager
{
    public class ManagerHomeRequestDto
    {
        public ManagerListRequestDto Warehouses { get; set; } = new();
        public ManagerListRequestDto Deliverers { get; set; } = new();
    }
}
