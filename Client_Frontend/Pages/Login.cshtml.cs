using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace Client_Frontend.Pages
{
    public class LoginModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public LoginModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [BindProperty]
        public LoginInput Input { get; set; } = new();

        public string? Message { get; set; }

        public class LoginInput
        {
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        // Server-side handler: acts as a backend-for-frontend proxy call to the API (avoids CORS)
        public async Task<IActionResult> OnPostServerLoginAsync()
        {
            var client = _httpClientFactory.CreateClient("Api");
            var payload = new { username = Input.Username, password = Input.Password };

            var resp = await client.PostAsJsonAsync("auth/login", payload);

            if (!resp.IsSuccessStatusCode)
            {
                Message = $"Login failed: {resp.StatusCode}";
                return Page();
            }

            // Read response JSON and check for role == NewUser (enum value 4)
            try
            {
                using var stream = await resp.Content.ReadAsStreamAsync();
                var doc = await JsonDocument.ParseAsync(stream);

                int roleValue = -1;
                string? usernameFromResp = null;

                if (doc.RootElement.TryGetProperty("role", out var roleEl) && roleEl.ValueKind == JsonValueKind.Number)
                {
                    roleEl.TryGetInt32(out roleValue);
                }

                if (doc.RootElement.TryGetProperty("username", out var unameEl) && unameEl.ValueKind == JsonValueKind.String)
                {
                    usernameFromResp = unameEl.GetString();
                }

                // NewUser enum value is 4 in the shared API
                if (roleValue == 4)
                {
                    // preserve username for the ChooseRole page
                    TempData["username"] = usernameFromResp ?? Input.Username;
                    return RedirectToPage("/ChooseRole");
                }
            }
            catch
            {
                // If parsing fails, continue and show success message
            }

            var json = await resp.Content.ReadFromJsonAsync<object>();
            Message = "Login successful (server-side).";
            // Optionally persist tokens from json here (session/cookie) as needed.
            return Page();
        }
    }
}
