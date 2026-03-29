using DeliveryWebLoL.DTO.Common;
using DeliveryWebLoL.DTO.Manager;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Client_Frontend.Pages
{
    public class ManagerHomeModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ManagerHomeModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public PageResponseDto<ManagerWarehouseDto>? Warehouses { get; private set; }
        public PageResponseDto<ManagerDelivererDto>? Deliverers { get; private set; }

        public async Task OnGetAsync(int page = 1, int pageSize = 20, string? search = null)
        {
            var client = _httpClientFactory.CreateClient("Api");

            var token = HttpContext.Session.GetString("access_token");
            if (!string.IsNullOrWhiteSpace(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var q = $"page={page}&pageSize={pageSize}" + (string.IsNullOrWhiteSpace(search) ? "" : $"&search={Uri.EscapeDataString(search)}");

            Warehouses = await client.GetFromJsonAsync<PageResponseDto<ManagerWarehouseDto>>($"manager/warehouses?{q}");
            Deliverers = await client.GetFromJsonAsync<PageResponseDto<ManagerDelivererDto>>($"manager/deliverers?{q}");
        }
    }
}
