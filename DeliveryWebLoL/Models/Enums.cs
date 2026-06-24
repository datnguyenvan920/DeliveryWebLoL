// Enum definitions for roles, types and statuses
namespace DeliveryWebLoL.Models
{
    public enum UserRole
    {
        Admin,
        Manager,
        Affiliate,
        Driver,
        NewUser
    }

    public enum LocationType
    {
        Factory,
        Warehouse,
        Affiliate
    }

    public enum ItemCategory
    {
        RawIngredient,
        FinishedProduct
    }

    public enum OrderType
    {
        Import_Ingredient,
        Export_Product,
        Return_Defect
    }

    public enum OrderStatus
    {
        Pending,
        Approved,
        Preparing,
        ReadyForPickup,
        InTransit,
        Delivered,
        Completed,
        Cancelled
    }

    public enum DeliveryStatus
    {
        Assigned,
        InProgress,
        Completed
    }

    public enum StopStatus
    {
        Pending,
        Arrived,
        HandedOver,
        Failed
    }
}
