using DeliveryWebLoL.DTO.Affiliate;

namespace DeliveryWebLoL.Service.Interfaces
{
    public interface IAffiliateService
    {
        Task<Guid?> ResolveMyWarehouseAsync(Guid affiliateUserId);
        Task<AffiliateContextDto?> GetMyContextAsync(Guid affiliateUserId);
        Task<IReadOnlyList<AffiliateWarehouseItemDto>> GetMyWarehouseItemsAsync(Guid affiliateUserId);
        Task<IReadOnlyList<AffiliateOrderDto>> GetMyOrdersAsync(Guid affiliateUserId);
        Task<AffiliateOrderDto> CreateOrderAsync(Guid affiliateUserId, CreateAffiliateOrderRequestDto req);
        Task<bool> CompleteOrderAsync(Guid affiliateUserId, Guid orderId);
        Task<bool> CancelOrderAsync(Guid affiliateUserId, Guid orderId);
    }
}
