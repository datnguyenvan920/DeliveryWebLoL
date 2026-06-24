using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Client_Frontend.Pages
{
    public class DriverDashboardModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public DriverDashboardModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public List<DriverOrderVm> Orders { get; set; } = new();

        public int ReadyCount { get; private set; }
        public int InTransitCount { get; private set; }
        public int DeliveredCount { get; private set; }

        private HttpClient CreateAuthorizedClient()
        {
            var client = _httpClientFactory.CreateClient("Api");
            var token = HttpContext.Session.GetString("access_token");
            if (!string.IsNullOrWhiteSpace(token))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        public async Task OnGetAsync(int take = 200)
        {
            var client = CreateAuthorizedClient();
            var rows = await client.GetFromJsonAsync<List<ApiDriverOrderDto>>($"driver/orders?take={take}")
                       ?? new List<ApiDriverOrderDto>();

            Orders = rows.Select(o => new DriverOrderVm
            {
                OrderId = o.OrderId.ToString(),
                CreatedAt = o.CreatedAt,
                Status = o.Status,
                StatusText = MapStatus(o.Status),
                StatusKey = MapStatusKey(o.Status),
                Source = string.IsNullOrWhiteSpace(o.SourceLocationName) ? o.SourceLocationId.ToString() : o.SourceLocationName,
                Destination = string.IsNullOrWhiteSpace(o.DestinationLocationName) ? o.DestinationLocationId.ToString() : o.DestinationLocationName,
                RequestedBy = o.RequestedByUsername,
                Items = o.Items?.Select(li => new DriverOrderLineVm { SKU = li.SKU, Name = li.Name, Unit = li.Unit, Quantity = li.Quantity }).ToList() ?? new()
            }).ToList();

            ReadyCount = Orders.Count(x => x.Status == 3);
            InTransitCount = Orders.Count(x => x.Status == 4);
            DeliveredCount = Orders.Count(x => x.Status == 5);

            ViewData["ReadyCount"] = ReadyCount;
            ViewData["InTransitCount"] = InTransitCount;
            ViewData["DeliveredCount"] = DeliveredCount;
        }

        public async Task<IActionResult> OnPostPickupAsync([FromForm] string orderId)
        {
            if (!Guid.TryParse(orderId, out var oid) || oid == Guid.Empty)
                return new JsonResult(new { success = false, message = "Invalid order id." }) { StatusCode = 400 };

            var client = CreateAuthorizedClient();
            var resp = await client.PostAsJsonAsync("driver/orders/status", new { orderId = oid, newStatus = 4 });
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                return new JsonResult(new { success = false, message = body }) { StatusCode = (int)resp.StatusCode };

            return new JsonResult(new { success = true, message = "Picked up" });
        }

        public async Task<IActionResult> OnPostDeliverAsync([FromForm] string orderId)
        {
            if (!Guid.TryParse(orderId, out var oid) || oid == Guid.Empty)
                return new JsonResult(new { success = false, message = "Invalid order id." }) { StatusCode = 400 };

            var client = CreateAuthorizedClient();
            var resp = await client.PostAsJsonAsync("driver/orders/status", new { orderId = oid, newStatus = 5 });
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                return new JsonResult(new { success = false, message = body }) { StatusCode = (int)resp.StatusCode };

            return new JsonResult(new { success = true, message = "Delivered" });
        }

        private static string MapStatus(int s) => s switch
        {
            0 => "Pending",
            1 => "Approved",
            2 => "Preparing",
            3 => "ReadyForPickup",
            4 => "InTransit",
            5 => "Delivered",
            6 => "Completed",
            7 => "Cancelled",
            _ => "Pending"
        };

        private static string MapStatusKey(int s) => s switch
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

        public class DriverOrderVm
        {
            public string OrderId { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
            public int Status { get; set; }
            public string StatusText { get; set; } = string.Empty;
            public string StatusKey { get; set; } = "pending";
            public string Source { get; set; } = string.Empty;
            public string Destination { get; set; } = string.Empty;
            public string RequestedBy { get; set; } = string.Empty;
            public List<DriverOrderLineVm> Items { get; set; } = new();
        }

        public class DriverOrderLineVm
        {
            public string SKU { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string? Unit { get; set; }
            public decimal Quantity { get; set; }
        }

        private class ApiDriverOrderDto
        {
            public Guid OrderId { get; set; }
            public DateTime CreatedAt { get; set; }
            public int Status { get; set; }
            public int OrderType { get; set; }
            public Guid SourceLocationId { get; set; }
            public string SourceLocationName { get; set; } = string.Empty;
            public Guid DestinationLocationId { get; set; }
            public string DestinationLocationName { get; set; } = string.Empty;
            public string RequestedByUsername { get; set; } = string.Empty;
            public List<ApiDriverOrderLineItemDto> Items { get; set; } = new();
        }

        private class ApiDriverOrderLineItemDto
        {
            public Guid ItemId { get; set; }
            public string SKU { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string? Unit { get; set; }
            public decimal Quantity { get; set; }
        }
    }
}
