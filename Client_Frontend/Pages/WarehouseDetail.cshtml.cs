using Client_Frontend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Client_Frontend.Pages
{
    public class WarehouseDetailModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public WarehouseDetailModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [BindProperty(SupportsGet = true)]
        public Guid Id { get; set; }

        public WarehouseDto? Warehouse { get; private set; }
        public string OwnerUsername { get; private set; } = string.Empty;
        public List<WarehouseItemDto> Items { get; private set; } = new();

        private HttpClient CreateAuthorizedClient()
        {
            var client = _httpClientFactory.CreateClient("Api");
            var token = HttpContext.Session.GetString("access_token");
            if (!string.IsNullOrWhiteSpace(token))
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        // ── GET ────────────────────────────────────────────────────────
        public async Task<IActionResult> OnGetAsync()
        {
            if (Id == Guid.Empty) return RedirectToPage("/ManagerHome");

            var client = CreateAuthorizedClient();

            // Best-effort: apply any pending production ticks before showing quantities
            try { await client.PostAsync($"production/apply?locationId={Id}", content: null); }
            catch { /* non-critical */ }

            // Load all owned warehouses and find the one matching the route Id
            var all = await client.GetFromJsonAsync<PageResponse<WarehouseDto>>(
                "manager/warehouses?pageSize=200");

            Warehouse = all?.Items?.FirstOrDefault(w => w.LocationID == Id);
            if (Warehouse == null) return RedirectToPage("/ManagerHome");

            // Load items that belong to this warehouse
            try
            {
                var resp = await client.GetAsync($"manager/warehouse-items?warehouseLocationId={Id}");
                if (resp.IsSuccessStatusCode)
                {
                    var body = await resp.Content.ReadAsStringAsync();
                    Items = JsonSerializer.Deserialize<List<WarehouseItemDto>>(
                        body,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                }
            }
            catch { /* page can still render without items */ }

            OwnerUsername = HttpContext.Session.GetString("username") ?? "—";
            return Page();
        }

        // ── POST: Create warehouse item (BFF proxy) ────────────────────
        public async Task<IActionResult> OnPostCreateItemAsync(
            [FromForm] string warehouseId,
            [FromForm] string name,
            [FromForm] string sku,
            [FromForm] int itemCategory,
            [FromForm] decimal initialQuantity,
            [FromForm] decimal unitsPerMinute,
            [FromForm] bool isProductionEnabled,
            [FromForm] string? unit)
        {
            if (!Guid.TryParse(warehouseId, out var warehouseGuid))
                return new JsonResult(new { success = false, message = "Invalid warehouse ID." }) { StatusCode = 400 };
            if (string.IsNullOrWhiteSpace(name))
                return new JsonResult(new { success = false, message = "Name is required." }) { StatusCode = 400 };
            if (string.IsNullOrWhiteSpace(sku))
                return new JsonResult(new { success = false, message = "SKU is required." }) { StatusCode = 400 };

            var client = CreateAuthorizedClient();
            var payload = new CreateWarehouseItemRequest
            {
                WarehouseLocationId = warehouseGuid,
                Name = name,
                SKU = sku,
                ItemCategory = itemCategory,
                InitialQuantity = initialQuantity,
                UnitsPerMinute = unitsPerMinute,
                IsProductionEnabled = isProductionEnabled,
                Unit = string.IsNullOrWhiteSpace(unit) ? null : unit
            };

            var resp = await client.PostAsJsonAsync("manager/warehouse-item", payload);
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                return new JsonResult(new
                {
                    success = false,
                    message = $"API error {(int)resp.StatusCode}",
                    details = body
                })
                { StatusCode = (int)resp.StatusCode };
            }

            CreateWarehouseItemResponse? created = null;
            try
            {
                created = JsonSerializer.Deserialize<CreateWarehouseItemResponse>(
                    body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch { /* fall through — best effort */ }

            return new JsonResult(new
            {
                success = true,
                message = $"Item \"{name}\" added successfully.",
                item = created,
                details = body
            });
        }

        // ── POST: Update item (BFF) ───────────────────────────────────
        public async Task<IActionResult> OnPostUpdateItemAsync(
            [FromForm] string warehouseId,
            [FromForm] string itemId,
            [FromForm] string name,
            [FromForm] decimal unitsPerMinute,
            [FromForm] bool isEnabled,
            [FromForm] string? unit)
        {
            if (!Guid.TryParse(warehouseId, out var warehouseGuid))
                return new JsonResult(new { success = false, message = "Invalid warehouse ID." }) { StatusCode = 400 };
            if (!Guid.TryParse(itemId, out var itemGuid))
                return new JsonResult(new { success = false, message = "Invalid item ID." }) { StatusCode = 400 };
            if (string.IsNullOrWhiteSpace(name))
                return new JsonResult(new { success = false, message = "Name is required." }) { StatusCode = 400 };
            if (unitsPerMinute < 0)
                return new JsonResult(new { success = false, message = "UnitsPerMinute must be >= 0." }) { StatusCode = 400 };

            var client = CreateAuthorizedClient();
            var payload = new UpdateWarehouseItemRequest
            {
                WarehouseLocationId = warehouseGuid,
                ItemId = itemGuid,
                Name = name,
                Unit = string.IsNullOrWhiteSpace(unit) ? null : unit,
                UnitsPerMinute = unitsPerMinute,
                IsEnabled = isEnabled
            };

            var req = new HttpRequestMessage(HttpMethod.Put, "manager/warehouse-item")
            {
                Content = JsonContent.Create(payload)
            };

            var resp = await client.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                return new JsonResult(new
                {
                    success = false,
                    message = $"API error {(int)resp.StatusCode}",
                    details = body
                })
                { StatusCode = (int)resp.StatusCode };
            }

            return new JsonResult(new
            {
                success = true,
                message = "Item updated.",
                details = body
            });
        }

        // ── POST: Update warehouse info (BFF proxy) ──────────────────────
        public async Task<IActionResult> OnPostUpdateWarehouseAsync(
            [FromForm] string warehouseId,
            [FromForm] string name,
            [FromForm] string? address,
            [FromForm] string? latitude,
            [FromForm] string? longitude)
        {
            if (!Guid.TryParse(warehouseId, out var wid) || wid == Guid.Empty)
                return new JsonResult(new { success = false, message = "Invalid warehouse id." }) { StatusCode = 400 };

            if (string.IsNullOrWhiteSpace(name))
                return new JsonResult(new { success = false, message = "Name is required." }) { StatusCode = 400 };

            double? lat = null;
            double? lng = null;
            if (!string.IsNullOrWhiteSpace(latitude))
            {
                if (!double.TryParse(latitude, out var v))
                    return new JsonResult(new { success = false, message = "Invalid latitude." }) { StatusCode = 400 };
                lat = v;
            }
            if (!string.IsNullOrWhiteSpace(longitude))
            {
                if (!double.TryParse(longitude, out var v))
                    return new JsonResult(new { success = false, message = "Invalid longitude." }) { StatusCode = 400 };
                lng = v;
            }

            var client = CreateAuthorizedClient();
            var payload = new
            {
                warehouseLocationId = wid,
                name = name,
                address = address,
                latitude = lat,
                longitude = lng
            };

            var req = new HttpRequestMessage(HttpMethod.Put, "manager/warehouse")
            {
                Content = JsonContent.Create(payload)
            };

            var resp = await client.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                // API might return plain text; always wrap to JSON for the browser.
                return new JsonResult(new
                {
                    success = false,
                    message = string.IsNullOrWhiteSpace(body) ? "API error" : body,
                    status = (int)resp.StatusCode
                }) { StatusCode = (int)resp.StatusCode };
            }

            return new JsonResult(new { success = true, message = "Warehouse updated" });
        }
    }
}
