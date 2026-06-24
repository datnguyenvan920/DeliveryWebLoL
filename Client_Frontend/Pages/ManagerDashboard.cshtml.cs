using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Client_Frontend.Pages
{
    public class ManagerDashboardModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ManagerDashboardModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public List<WarehouseVm> Warehouses { get; private set; } = new();
        public int TotalOrders { get; private set; }
        public int OrdersToday { get; private set; }
        public int ActiveItems { get; private set; }

        private HttpClient CreateAuthorizedClient()
        {
            var client = _httpClientFactory.CreateClient("Api");
            var token = HttpContext.Session.GetString("access_token");
            if (!string.IsNullOrWhiteSpace(token))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        public async Task OnGetAsync(int takeOrders = 500)
        {
            var client = CreateAuthorizedClient();

            // Warehouses
            var whPage = await client.GetFromJsonAsync<PageResponse<WarehouseVm>>("manager/warehouses?pageSize=200");
            Warehouses = whPage?.Items ?? new List<WarehouseVm>();

            // Orders for KPIs
            var orders = await client.GetFromJsonAsync<List<ApiManagerOrderDto>>($"manager/orders?take={takeOrders}")
                         ?? new List<ApiManagerOrderDto>();

            TotalOrders = orders.Count;
            var today = DateTime.UtcNow.Date;
            OrdersToday = orders.Count(o => o.CreatedAt.Date == today);

            // Active items (unique items across all owned warehouses)
            var itemIds = new HashSet<Guid>();
            foreach (var w in Warehouses)
            {
                if (w.LocationID == Guid.Empty) continue;
                try
                {
                    var items = await client.GetFromJsonAsync<List<ApiWarehouseItemDto>>($"manager/warehouse-items?warehouseLocationId={w.LocationID}")
                                ?? new List<ApiWarehouseItemDto>();
                    foreach (var it in items) itemIds.Add(it.ItemId);
                }
                catch
                {
                    // ignore per-warehouse failures
                }
            }

            ActiveItems = itemIds.Count;
        }

        public class WarehouseVm
        {
            public Guid LocationID { get; set; }
            public string Name { get; set; } = string.Empty;
            public string? Address { get; set; }
            public double? Latitude { get; set; }
            public double? Longitude { get; set; }
        }

        private class ApiWarehouseItemDto
        {
            public Guid ItemId { get; set; }
            public string SKU { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public int ItemCategory { get; set; }
            public string? Unit { get; set; }
            public decimal Quantity { get; set; }
            public decimal UnitsPerMinute { get; set; }
            public bool IsProductionEnabled { get; set; }
            public DateTime LastUpdated { get; set; }
        }

        private class ApiManagerOrderDto
        {
            public Guid OrderId { get; set; }
            public DateTime CreatedAt { get; set; }
            public int Status { get; set; }
        }

        private class PageResponse<T>
        {
            public List<T> Items { get; set; } = new();
            public int Total { get; set; }
            public int Page { get; set; }
            public int PageSize { get; set; }
        }
    }
}
