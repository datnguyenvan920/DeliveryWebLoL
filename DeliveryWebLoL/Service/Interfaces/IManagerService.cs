using DeliveryWebLoL.DTO.Common;
using DeliveryWebLoL.DTO.Manager;

namespace DeliveryWebLoL.Service.Interfaces
{
    public interface IManagerService
    {
        Task<PageResponseDto<ManagerWarehouseDto>> GetOwnedWarehousesAsync(Guid managerUserId, ManagerListRequestDto req);

        // Deliverers are users with Role == Driver (3) and AffiliationId points to an Affiliate
        // that is linked to the warehouse (Location) owned by this manager.
        Task<PageResponseDto<ManagerDelivererDto>> GetDeliverersForOwnedWarehousesAsync(Guid managerUserId, ManagerListRequestDto req);

        Task<PageResponseDto<ManagerDelivererDto>> GetAffiliateUsersForOwnedWarehousesAsync(Guid managerUserId, ManagerListRequestDto req);

        /// <summary>
        /// Returns the LocationIDs (warehouses) owned by the given manager user that currently have a row
        /// in AffiliateWarehouses (i.e., they are linked to at least one affiliation).
        /// </summary>
        Task<IReadOnlyList<Guid>> GetOwnedWarehouseIdsWithAffiliateWarehouseLinkAsync(Guid managerUserId);

        Task<ManagerWarehouseDto> AddWarehouseAsync(Guid managerUserId, AddWarehouseRequestDto req);

        // Creates an Affiliate (Affiliation) linked to the chosen warehouse and assigns the deliverer to it
        // by setting User.AffiliationId = created Affiliate.AffiliationId.
        Task<bool> AddDelivererAsync(Guid managerUserId, AddDelivererRequestDto req);

        Task<bool> UpdateWarehouseAsync(Guid managerUserId, UpdateWarehouseRequestDto req);
    }
}
