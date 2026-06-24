using Client_Frontend.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Headers;

namespace Client_Frontend.Pages
{
    public class AffiliatesModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public AffiliatesModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public PageResponse<DelivererDto>? Deliverers { get; private set; }
        public PageResponse<DelivererDto>? Affiliators { get; private set; }

        private HttpClient CreateAuthorizedClient()
        {
            var client = _httpClientFactory.CreateClient("Api");
            var token = HttpContext.Session.GetString("access_token");
            if (!string.IsNullOrWhiteSpace(token))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        public async Task OnGetAsync(int page = 1, int pageSize = 50, string? search = null)
        {
            var client = CreateAuthorizedClient();
            var q = $"page={page}&pageSize={pageSize}" + (string.IsNullOrWhiteSpace(search) ? "" : $"&search={Uri.EscapeDataString(search)}");

            // Drivers assigned to affiliation ids for warehouses owned by manager
            Deliverers = await client.GetFromJsonAsync<PageResponse<DelivererDto>>($"manager/deliverers?{q}");

            // Affiliate users with same affiliation ids
            Affiliators = await client.GetFromJsonAsync<PageResponse<DelivererDto>>($"manager/affiliate-users?{q}");
        }
    }
}
