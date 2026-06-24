using Client_Frontend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Client_Frontend.Pages
{
    public class ManagerHomeModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ManagerHomeModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public PageResponse<WarehouseDto>?  Warehouses { get; private set; }
        public PageResponse<DelivererDto>?  Deliverers { get; private set; }

        // ── Shared: create an authenticated HttpClient from session token ──
        private HttpClient CreateAuthorizedClient()
        {
            var client = _httpClientFactory.CreateClient("Api");
            var token = HttpContext.Session.GetString("access_token");
            if (!string.IsNullOrWhiteSpace(token))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        // ── GET: load page data ────────────────────────────────────────────
        public async Task OnGetAsync(int page = 1, int pageSize = 20, string? search = null)
        {
            var client = CreateAuthorizedClient();
            var q = $"page={page}&pageSize={pageSize}" + (string.IsNullOrWhiteSpace(search) ? "" : $"&search={Uri.EscapeDataString(search)}");

            Warehouses = await client.GetFromJsonAsync<PageResponse<WarehouseDto>>($"manager/warehouses?{q}");
            Deliverers = await client.GetFromJsonAsync<PageResponse<DelivererDto>>($"manager/deliverers?{q}");
        }

        // ── POST: Add Warehouse (BFF proxy) ───────────────────────────────
        public async Task<IActionResult> OnPostAddWarehouseAsync(
            [FromForm] string name,
            [FromForm] string? address,
            [FromForm] double? latitude,
            [FromForm] double? longitude)
        {
            if (string.IsNullOrWhiteSpace(name))
                return new JsonResult(new { success = false, message = "Name is required." }) { StatusCode = 400 };

            var client  = CreateAuthorizedClient();
            var payload = new AddWarehouseRequest
            {
                Name      = name,
                Address   = address,
                Latitude  = latitude,
                Longitude = longitude
            };

            var resp = await client.PostAsJsonAsync("manager/warehouse", payload);
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

            return new JsonResult(new { success = true, message = "Warehouse created.", details = body });
        }

        // ── POST: Add Affiliate / Deliverer (BFF proxy) ───────────────────
        public async Task<IActionResult> OnPostAddDelivererAsync(
            [FromForm] string warehouseLocationId,
            [FromForm] string? affiliatePrimaryLocationId)
        {
            if (!Guid.TryParse(warehouseLocationId, out var warehouseGuid))
                return new JsonResult(new { success = false, message = "Invalid Warehouse (must be a GUID)." }) { StatusCode = 400 };

            var client  = CreateAuthorizedClient();
            var payload = new AddDelivererRequest
            {
                WarehouseLocationId        = warehouseGuid,
                AffiliatePrimaryLocationId = Guid.TryParse(affiliatePrimaryLocationId, out var pg) ? pg : null
            };

            var resp = await client.PostAsJsonAsync("manager/deliverer", payload);
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

            return new JsonResult(new { success = true, message = "Affiliate created.", details = body });
        }
    }
}
