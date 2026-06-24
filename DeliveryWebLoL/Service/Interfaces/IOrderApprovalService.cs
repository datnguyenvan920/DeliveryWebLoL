using DeliveryWebLoL.DTO.Manager;

namespace DeliveryWebLoL.Service.Interfaces
{
    public interface IOrderApprovalService
    {
        Task<ManagerOrderDto?> GetOrderDetailForManagerAsync(Guid managerUserId, Guid orderId);
        Task<bool> ApproveOrderAsync(Guid managerUserId, Guid orderId);
    }
}
