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

            // Read content ONCE to avoid ObjectDisposedException (stream already consumed)
            var body = await resp.Content.ReadAsStringAsync();

            string? accessToken = null;
            int roleValue = -1;
            string? usernameFromResp = null;

            try
            {
                using var doc = JsonDocument.Parse(body);

                if (doc.RootElement.TryGetProperty("accessToken", out var atEl) && atEl.ValueKind == JsonValueKind.String)
                    accessToken = atEl.GetString();

                if (doc.RootElement.TryGetProperty("role", out var roleEl) && roleEl.ValueKind == JsonValueKind.Number)
                    roleEl.TryGetInt32(out roleValue);

                if (doc.RootElement.TryGetProperty("username", out var unameEl) && unameEl.ValueKind == JsonValueKind.String)
                    usernameFromResp = unameEl.GetString();
            }
            catch
            {
                // ignore parsing
            }

            // Store token + username in session for authorized calls
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                HttpContext.Session.SetString("access_token", accessToken);
            }
            HttpContext.Session.SetString("username", usernameFromResp ?? Input.Username);

            // NewUser enum value is 4
            if (roleValue == 4)
            {
                TempData["username"] = usernameFromResp ?? Input.Username;
                return RedirectToPage("/ChooseRole");
            }

            // If manager, go to manager home directly
            if (roleValue == 1)
            {
                return RedirectToPage("/ManagerHome");
            }

            Message = "Login successful (server-side).";
            return Page();
        }
    }
}
