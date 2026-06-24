using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace Client_Frontend.Pages
{
    public class RegisterModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public RegisterModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [BindProperty]
        public RegisterInput Input { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? ReturnUrl { get; set; }

        public string? Message { get; set; }

        public class RegisterInput
        {
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            public string PhoneNum { get; set; } = string.Empty;
            public string? Email { get; set; }
            public int Role { get; set; } = 4;
        }

        private static string? TryExtractMessage(string? body)
        {
            if (string.IsNullOrWhiteSpace(body)) return null;
            try
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    if (doc.RootElement.TryGetProperty("message", out var msg) && msg.ValueKind == JsonValueKind.String)
                        return msg.GetString();
                    if (doc.RootElement.TryGetProperty("error", out var err) && err.ValueKind == JsonValueKind.String)
                        return err.GetString();
                }
            }
            catch
            {
                // non-JSON body
            }

            return body.Length > 400 ? body[..400] + "…" : body;
        }

        // Server-side registration proxy
        public async Task<IActionResult> OnPostServerRegisterAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var client = _httpClientFactory.CreateClient("Api");
            var payload = new
            {
                username = Input.Username,
                password = Input.Password,
                phoneNum = Input.PhoneNum,
                email = Input.Email,
                role = Input.Role
            };

            var resp = await client.PostAsJsonAsync("/auth/register", payload);
            var raw = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                var extracted = TryExtractMessage(raw);
                Message = extracted ?? $"Register failed: {(int)resp.StatusCode} {resp.ReasonPhrase}";
                return Page();
            }

            // On success: go to return url if safe, otherwise to Login.
            if (!string.IsNullOrWhiteSpace(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
                return LocalRedirect(ReturnUrl);

            return RedirectToPage("/Login", new { registered = "1" });
        }
    }
}
