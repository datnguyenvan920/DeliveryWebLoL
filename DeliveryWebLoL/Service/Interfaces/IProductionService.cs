namespace DeliveryWebLoL.Service.Interfaces
{
    public interface IProductionService
    {
        Task ApplyProductionAsync(Guid locationId, Guid itemId, DateTime? nowUtc = null);
        Task UpdateProductionRateAsync(Guid locationId, Guid itemId, decimal unitsPerMinute, bool isEnabled, DateTime? nowUtc = null);
    }
}
