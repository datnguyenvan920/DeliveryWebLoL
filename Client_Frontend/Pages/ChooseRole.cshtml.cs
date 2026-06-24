using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Client_Frontend.Pages
{
    public class ChooseRoleModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ChooseRoleModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [BindProperty]
        public int SelectedRole { get; set; }

        [BindProperty]
        public string Extra { get; set; } = string.Empty;

        public string? Message { get; set; }

        public IActionResult OnGet()
        {
            if (!TempData.ContainsKey("username")) return RedirectToPage("/Login");
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!TempData.TryGetValue("username", out var usernameObj)) return RedirectToPage("/Login");
            var username = usernameObj as string ?? string.Empty;
            if (string.IsNullOrWhiteSpace(username)) return RedirectToPage("/Login");

            var client = _httpClientFactory.CreateClient("Api");
            var payload = new { username = username, role = SelectedRole, extra = Extra ?? string.Empty };
            var resp = await client.PostAsJsonAsync("auth/set-role", payload);
            if (!resp.IsSuccessStatusCode)
            {
                Message = $"Failed: {resp.StatusCode}";
                return Page();
            }

            // If the manager role (1) selected, redirect directly to manager home
            if (SelectedRole == 1)
            {
                // clear TempData and go to manager home
                TempData.Remove("username");
                return RedirectToPage("/ManagerHome");
            }

            if (SelectedRole == 3)
            {
                TempData.Remove("username");
                return RedirectToPage("/DriverDashboard");
            }

            if (SelectedRole == 2)
            {
                TempData.Remove("username");
                return RedirectToPage("/AffiliateDashboard");
            }

            Message = "Role updated successfully. Please login again.";
            // Clear TempData so user won't return here unexpectedly
            TempData.Remove("username");
            return Page();
        }
    }
}
