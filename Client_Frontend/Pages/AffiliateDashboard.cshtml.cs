using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Client_Frontend.Pages
{
    public class AffiliateDashboardModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public AffiliateDashboardModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public List<AffiliateOrderVm> Orders { get; private set; } = new();
        public List<WarehouseItemVm> WarehouseItems { get; private set; } = new();

        public Guid? DestinationLocationId { get; private set; }
        public Guid? WarehouseLocationId { get; private set; }
        public string WarehouseName { get; private set; } = string.Empty;

        public bool NeedAffiliationClaim => !WarehouseLocationId.HasValue;

        [BindProperty]
        public string ClaimCode { get; set; } = string.Empty;

        [TempData]
        public string? ClaimMessage { get; set; }

        private HttpClient CreateAuthorizedClient()
        {
            var client = _httpClientFactory.CreateClient("Api");
            var token = HttpContext.Session.GetString("access_token");
            if (!string.IsNullOrWhiteSpace(token))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        public async Task<IActionResult> OnPostClaimAffiliationAsync()
        {
            var client = CreateAuthorizedClient();

            if (string.IsNullOrWhiteSpace(ClaimCode) || !Guid.TryParse(ClaimCode.Trim(), out var code) || code == Guid.Empty)
            {
                ClaimMessage = "Invalid code. Please paste the affiliate location GUID.";
                return RedirectToPage();
            }

            var resp = await client.PostAsJsonAsync("auth/claim-affiliation", new { affiliateLocationCode = code });
            if (!resp.IsSuccessStatusCode)
            {
                var txt = await resp.Content.ReadAsStringAsync();
                ClaimMessage = string.IsNullOrWhiteSpace(txt) ? "Unable to claim affiliation." : txt;
                return RedirectToPage();
            }

            ClaimMessage = "✓ Affiliation claimed successfully! Your dashboard is now active.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostCreateOrderAsync(
            [FromForm] string sourceWarehouseLocationId,
            [FromForm] string destinationLocationId,
            [FromForm] string itemsJson)
        {
            if (!Guid.TryParse(sourceWarehouseLocationId, out var srcGuid) || srcGuid == Guid.Empty)
                return new JsonResult(new { success = false, message = "Invalid source warehouse ID." }) { StatusCode = 400 };

            Guid destGuid;
            if (!Guid.TryParse(destinationLocationId, out destGuid) || destGuid == Guid.Empty)
            {
                var clientCtx = CreateAuthorizedClient();
                var ctx = await clientCtx.GetFromJsonAsync<ApiAffiliateContextDto>("affiliate/context");
                if (ctx?.DestinationLocationId == null || ctx.DestinationLocationId == Guid.Empty)
                    return new JsonResult(new { success = false, message = "Missing destination location for your account." }) { StatusCode = 400 };
                destGuid = ctx.DestinationLocationId.Value;
            }

            List<CreateOrderLineItem>? lines;
            try
            {
                lines = System.Text.Json.JsonSerializer.Deserialize<List<CreateOrderLineItem>>(
                    itemsJson,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch
            {
                return new JsonResult(new { success = false, message = "Invalid items payload." }) { StatusCode = 400 };
            }

            if (lines == null || lines.Count == 0)
                return new JsonResult(new { success = false, message = "At least one item is required." }) { StatusCode = 400 };

            // Basic validation; prevent sending nonsense / negative
            if (lines.Any(l => l.ItemId == Guid.Empty || l.Quantity <= 0))
                return new JsonResult(new { success = false, message = "All quantities must be > 0." }) { StatusCode = 400 };

            // Validate against current availability to avoid server exceptions
            var client = CreateAuthorizedClient();
            var items = await client.GetFromJsonAsync<List<ApiWarehouseItemDto>>("affiliate/warehouse-items")
                        ?? new List<ApiWarehouseItemDto>();

            var availByItem = items.ToDictionary(i => i.ItemId, i => i.Quantity);
            foreach (var li in lines)
            {
                if (!availByItem.TryGetValue(li.ItemId, out var avail))
                    return new JsonResult(new { success = false, message = "One or more items are not available in the warehouse." }) { StatusCode = 400 };

                if (li.Quantity > avail)
                    return new JsonResult(new { success = false, message = $"Quantity exceeds available stock (available: {avail})." }) { StatusCode = 400 };
            }

            const int exportProductOrderType = 1;

            var payload = new
            {
                sourceWarehouseLocationId = srcGuid,
                destinationLocationId = destGuid,
                orderType = exportProductOrderType,
                items = lines.Select(l => new { itemId = l.ItemId, quantity = l.Quantity })
            };

            var resp = await client.PostAsJsonAsync("affiliate/order", payload);

            if (!resp.IsSuccessStatusCode)
            {
                // Try to parse common API error shape { message: "..." }
                string msg = $"API error {(int)resp.StatusCode}";
                try
                {
                    var dict = await resp.Content.ReadFromJsonAsync<Dictionary<string, object>>();
                    if (dict != null && dict.TryGetValue("message", out var mObj) && mObj != null)
                        msg = mObj.ToString() ?? msg;
                    else
                        msg = await resp.Content.ReadAsStringAsync();
                }
                catch
                {
                    msg = await resp.Content.ReadAsStringAsync();
                }

                // Normalize stock/validation failures to 400 for the UI
                var status = (int)resp.StatusCode >= 500 ? 400 : (int)resp.StatusCode;
                return new JsonResult(new { success = false, message = msg }) { StatusCode = status };
            }

            return new JsonResult(new { success = true, message = "Order created successfully." });
        }

        public async Task<IActionResult> OnPostCompleteOrderAsync([FromForm] string orderId)
        {
            if (string.IsNullOrWhiteSpace(orderId) || !Guid.TryParse(orderId, out var oid) || oid == Guid.Empty)
                return new JsonResult(new { success = false, message = "Invalid order id." }) { StatusCode = 400 };

            var client = CreateAuthorizedClient();
            var resp = await client.PostAsJsonAsync("affiliate/orders/complete", new { orderId = oid });
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                return new JsonResult(new { success = false, message = body }) { StatusCode = (int)resp.StatusCode };

            return new JsonResult(new { success = true, message = "Order completed" });
        }

        public async Task<IActionResult> OnPostCancelOrderAsync([FromForm] string orderId)
        {
            if (string.IsNullOrWhiteSpace(orderId) || !Guid.TryParse(orderId, out var oid) || oid == Guid.Empty)
                return new JsonResult(new { success = false, message = "Invalid order id." }) { StatusCode = 400 };

            var client = CreateAuthorizedClient();
            var resp = await client.PostAsJsonAsync("affiliate/orders/cancel", new { orderId = oid });
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                return new JsonResult(new { success = false, message = body }) { StatusCode = (int)resp.StatusCode };

            return new JsonResult(new { success = true, message = "Order cancelled" });
        }

        private class CreateOrderLineItem
        {
            public Guid ItemId { get; set; }
            public decimal Quantity { get; set; }
        }


        public async Task OnGetAsync()
        {
            var client = CreateAuthorizedClient();

            var ctx = await client.GetFromJsonAsync<ApiAffiliateContextDto>("affiliate/context");
            DestinationLocationId = ctx?.DestinationLocationId;
            WarehouseLocationId = ctx != null && ctx.WarehouseLocationId != Guid.Empty ? ctx.WarehouseLocationId : null;
            WarehouseName = ctx?.WarehouseName ?? string.Empty;

            // Auto-apply production to keep warehouse inventory up to date.
            if (WarehouseLocationId.HasValue)
            {
                try
                {
                    await client.PostAsync($"production/apply?locationId={WarehouseLocationId.Value}", null);
                }
                catch
                {
                    // ignore (still render whatever inventory is there)
                }
            }

            var orders = await client.GetFromJsonAsync<List<ApiAffiliateOrderDto>>("affiliate/orders")
                         ?? new List<ApiAffiliateOrderDto>();

            Orders = orders.Select(Map).ToList();

            var items = await client.GetFromJsonAsync<List<ApiWarehouseItemDto>>("affiliate/warehouse-items")
                        ?? new List<ApiWarehouseItemDto>();

            WarehouseItems = items.Select(i => new WarehouseItemVm
            {
                ItemId = i.ItemId,
                SKU = i.SKU,
                Name = i.Name,
                Unit = i.Unit,
                Quantity = i.Quantity,
                LastUpdated = i.LastUpdated
            }).ToList();
        }

        private static AffiliateOrderVm Map(ApiAffiliateOrderDto o)
        {
            var statusKey = o.Status switch
            {
                0 => "pending",
                1 => "approved",
                2 => "preparing",
                3 => "ready",
                4 => "intransit",
                5 => "delivered",
                6 => "completed",
                7 => "cancelled",
                _ => "pending"
            };

            return new AffiliateOrderVm
            {
                OrderId = o.OrderId.ToString(),
                CreatedAt = o.CreatedAt,
                Destination = o.DestinationLocationId.ToString(),
                OrderType = o.OrderType.ToString(),
                Status = statusKey,
                StatusKey = statusKey,
                BadgeCss = statusKey switch
                {
                    "pending" => "af-pending",
                    "approved" => "af-approved",
                    "preparing" => "af-preparing",
                    "ready" => "af-ready",
                    "intransit" => "af-intransit",
                    "delivered" => "af-intransit",
                    "completed" => "af-completed",
                    "cancelled" => "af-cancelled",
                    _ => "af-pending"
                },
                Note = o.Items != null && o.Items.Count > 0
                    ? string.Join(", ", o.Items.Select(li => $"{li.SKU} x{li.Quantity}"))
                    : null
            };
        }

        private class ApiAffiliateContextDto
        {
            public Guid WarehouseLocationId { get; set; }
            public string WarehouseName { get; set; } = string.Empty;
            public Guid? DestinationLocationId { get; set; }
        }

        public class AffiliateOrderVm
        {
            public string OrderId { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
            public string OrderType { get; set; } = string.Empty;
            public string Destination { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public string StatusKey { get; set; } = "pending";
            public string BadgeCss { get; set; } = "af-pending";
            public string? Note { get; set; }
        }

        public class WarehouseItemVm
        {
            public Guid ItemId { get; set; }
            public string SKU { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string? Unit { get; set; }
            public decimal Quantity { get; set; }
            public DateTime LastUpdated { get; set; }
        }

        private class ApiWarehouseItemDto
        {
            public Guid ItemId { get; set; }
            public string SKU { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string? Unit { get; set; }
            public decimal Quantity { get; set; }
            public DateTime LastUpdated { get; set; }
        }

        private class ApiAffiliateOrderDto
        {
            public Guid OrderId { get; set; }
            public DateTime CreatedAt { get; set; }
            public int Status { get; set; }
            public int OrderType { get; set; }
            public Guid SourceLocationId { get; set; }
            public Guid DestinationLocationId { get; set; }
            public List<ApiAffiliateOrderLineItemDto> Items { get; set; } = new();
        }

        private class ApiAffiliateOrderLineItemDto
        {
            public Guid ItemId { get; set; }
            public string SKU { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string? Unit { get; set; }
            public decimal Quantity { get; set; }
        }
    }
}
