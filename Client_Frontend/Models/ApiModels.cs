// =================================================================
// Client_Frontend/Models — Local DTO definitions
//
// These mirror the shapes returned by the API server over HTTP.
// NO project reference to the server project is needed.
// All communication happens exclusively through HTTP (HttpClient).
// =================================================================

namespace Client_Frontend.Models
{
    // ── Shared pagination wrapper ─────────────────────────────────────
    public class PageResponse<T>
    {
        public List<T> Items { get; set; } = new();
        public int Total    { get; set; }
        public int Page     { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)Total / PageSize);
    }

    // ── Manager — Warehouse ───────────────────────────────────────────
    public class WarehouseDto
    {
        public Guid    LocationID   { get; set; }
        public string  Name         { get; set; } = string.Empty;
        public string? Address      { get; set; }
        public int     LocationType { get; set; }
    }

    // ── Manager — Deliverer / Affiliate ──────────────────────────────
    public class DelivererDto
    {
        public Guid    UserID        { get; set; }
        public string  Username      { get; set; } = string.Empty;
        public string? ContactPhone  { get; set; }
        public string? Email         { get; set; }
        public string? AffiliationId { get; set; }
        public int     Role          { get; set; }
    }

    // ── Manager — Warehouse Item ──────────────────────────────────────
    public class WarehouseItemDto
    {
        public Guid      ItemId              { get; set; }
        public string    SKU                 { get; set; } = string.Empty;
        public string    Name                { get; set; } = string.Empty;
        public int       ItemCategory        { get; set; }
        public string?   Unit                { get; set; }
        public decimal   Quantity            { get; set; }
        public decimal   UnitsPerMinute      { get; set; }
        public bool      IsProductionEnabled { get; set; }
        public DateTime? LastUpdated         { get; set; }
    }

    // ── Request payloads sent TO the API (serialised as JSON body) ────
    public class AddWarehouseRequest
    {
        public string  Name      { get; set; } = string.Empty;
        public string? Address   { get; set; }
        public double? Latitude  { get; set; }
        public double? Longitude { get; set; }
    }

    public class AddDelivererRequest
    {
        public Guid  WarehouseLocationId        { get; set; }
        public Guid? DelivererUserId            { get; set; }
        public Guid? AffiliatePrimaryLocationId { get; set; }
    }

    public class CreateWarehouseItemRequest
    {
        public Guid    WarehouseLocationId { get; set; }
        public string  SKU                { get; set; } = string.Empty;
        public string  Name               { get; set; } = string.Empty;
        public int     ItemCategory       { get; set; }
        public string? Unit               { get; set; }
        public decimal UnitsPerMinute     { get; set; }
        public bool    IsProductionEnabled{ get; set; } = true;
        public decimal InitialQuantity    { get; set; }
    }

    // ── Response shapes returned FROM the API ─────────────────────────
    public class CreateWarehouseItemResponse
    {
        public Guid    ItemId              { get; set; }
        public Guid    WarehouseLocationId { get; set; }
        public string  SKU                { get; set; } = string.Empty;
        public string  Name               { get; set; } = string.Empty;
        public int     ItemCategory       { get; set; }
        public string? Unit               { get; set; }
        public decimal InitialQuantity    { get; set; }
        public decimal UnitsPerMinute     { get; set; }
        public bool    IsProductionEnabled{ get; set; }
    }

    public class UpdateWarehouseItemRequest
    {
        public Guid    WarehouseLocationId { get; set; }
        public Guid    ItemId              { get; set; }
        public string  Name                { get; set; } = string.Empty;
        public string? Unit                { get; set; }
        public decimal UnitsPerMinute      { get; set; }
        public bool    IsEnabled           { get; set; }
    }
}
