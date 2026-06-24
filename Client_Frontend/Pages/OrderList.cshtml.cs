using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Client_Frontend.Pages
{
    public class OrderListModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public OrderListModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // Front-end only placeholder. Replace with API call when endpoints are available.
        public List<OrderRowVm> Orders { get; private set; } = new();

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

            var rows = await client.GetFromJsonAsync<List<ApiManagerOrderDto>>($"manager/orders?take={take}")
                       ?? new List<ApiManagerOrderDto>();

            Orders = rows.Select(o => new OrderRowVm
            {
                OrderId = o.OrderId.ToString(),
                CreatedAt = o.CreatedAt,
                Customer = string.IsNullOrWhiteSpace(o.RequestedByUsername) ? "—" : o.RequestedByUsername,
                Destination = string.IsNullOrWhiteSpace(o.DestinationLocationName) ? o.DestinationLocationId.ToString() : o.DestinationLocationName,
                Source = string.IsNullOrWhiteSpace(o.SourceLocationName) ? o.SourceLocationId.ToString() : o.SourceLocationName,
                Status = MapStatus(o.Status),
                StatusCss = MapStatusCss(o.Status),
                Total = 0m,
                ItemsSummary = o.Items != null && o.Items.Count > 0
                    ? string.Join(", ", o.Items.Select(li => $"{li.SKU} x{li.Quantity}"))
                    : "—",
                Items = o.Items?.Select(li => new OrderLineVm
                {
                    ItemId = li.ItemId,
                    SKU = li.SKU,
                    Name = li.Name,
                    Unit = li.Unit,
                    Quantity = li.Quantity
                }).ToList() ?? new List<OrderLineVm>()
            }).ToList();
        }

        private static string MapStatus(int s) => s switch
        {
            0 => "Pending",
            1 => "Approved",
            2 => "Preparing",
            3 => "Ready",
            4 => "InTransit",
            5 => "Delivered",
            6 => "Completed",
            7 => "Cancelled",
            _ => "Pending"
        };

        private static string MapStatusCss(int s) => s switch
        {
            0 => "new",
            1 => "inprog",
            2 => "inprog",
            3 => "inprog",
            4 => "inprog",
            5 => "done",
            6 => "done",
            7 => "cancel",
            _ => "new"
        };

        private class ApiManagerOrderDto
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
            public List<ApiManagerOrderLineItemDto> Items { get; set; } = new();
        }

        private class ApiManagerOrderLineItemDto
        {
            public Guid ItemId { get; set; }
            public string SKU { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string? Unit { get; set; }
            public decimal Quantity { get; set; }
        }

        public class OrderLineVm
        {
            public Guid ItemId { get; set; }
            public string SKU { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string? Unit { get; set; }
            public decimal Quantity { get; set; }
        }

        public class OrderRowVm
        {
            public string OrderId { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
            public string Customer { get; set; } = string.Empty;
            public string Destination { get; set; } = string.Empty;
            public string Source { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public string StatusCss { get; set; } = "new";
            public decimal Total { get; set; }
            public string ItemsSummary { get; set; } = string.Empty;
            public List<OrderLineVm> Items { get; set; } = new();
        }

        [BindProperty]
        public string OrderId { get; set; } = string.Empty;

        public async Task<IActionResult> OnPostApproveAsync([FromForm] string orderId)
        {
            if (string.IsNullOrWhiteSpace(orderId) || !Guid.TryParse(orderId, out var oid) || oid == Guid.Empty)
                return new JsonResult(new { success = false, message = "Invalid order id." }) { StatusCode = 400 };

            var client = CreateAuthorizedClient();
            var resp = await client.PostAsJsonAsync("manager/orders/approve", new { orderId = oid });
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                return new JsonResult(new { success = false, message = body }) { StatusCode = (int)resp.StatusCode };

            return new JsonResult(new { success = true, message = "Order approved" });
        }
    }
}
