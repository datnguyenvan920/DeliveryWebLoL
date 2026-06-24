using Client_Frontend.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Headers;

namespace Client_Frontend.Pages;

public class AffiliateGuidsModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public AffiliateGuidsModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public List<string> AffiliationIds { get; private set; } = new();

    public sealed record WarehouseLinkRow(Guid LocationId, string Name);
    public List<WarehouseLinkRow> LinkedWarehouses { get; private set; } = new();

    private HttpClient CreateAuthorizedClient()
    {
        var client = _httpClientFactory.CreateClient("Api");
        var token = HttpContext.Session.GetString("access_token");
        if (!string.IsNullOrWhiteSpace(token))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public async Task OnGetAsync(int page = 1, int pageSize = 200)
    {
        var client = CreateAuthorizedClient();

        // Reuse existing Manager endpoints (deliverers + affiliate-users) and fold into unique affiliation GUIDs.
        var q = $"page={page}&pageSize={pageSize}";

        var deliverers = await client.GetFromJsonAsync<PageResponse<DelivererDto>>($"manager/deliverers?{q}");
        var affiliators = await client.GetFromJsonAsync<PageResponse<DelivererDto>>($"manager/affiliate-users?{q}");

        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var d in deliverers?.Items ?? Enumerable.Empty<DelivererDto>())
        {
            if (!string.IsNullOrWhiteSpace(d.AffiliationId))
                ids.Add(d.AffiliationId);
        }
        foreach (var a in affiliators?.Items ?? Enumerable.Empty<DelivererDto>())
        {
            if (!string.IsNullOrWhiteSpace(a.AffiliationId))
                ids.Add(a.AffiliationId);
        }

        AffiliationIds = ids
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Warehouses owned by manager that are linked in AffiliateWarehouses
        var linkedWarehouseIds = await client.GetFromJsonAsync<List<Guid>>("manager/affiliate-warehouses") ?? new();

        // Fetch manager warehouses and filter down to those ids. (Keeps it simple and avoids new DTO/backend join code.)
        var allWarehouses = await client.GetFromJsonAsync<PageResponse<WarehouseDto>>($"manager/warehouses?page=1&pageSize=500");

        LinkedWarehouses = (allWarehouses?.Items ?? new List<WarehouseDto>())
            .Where(w => linkedWarehouseIds.Contains(w.LocationID))
            .OrderBy(w => w.Name)
            .Select(w => new WarehouseLinkRow(w.LocationID, w.Name))
            .ToList();
    }

    // Local view model for manager warehouses response
    public sealed class WarehouseDto
    {
        public Guid LocationID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public int LocationType { get; set; }
    }
}
