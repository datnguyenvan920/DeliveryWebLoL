using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

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

        public string? Message { get; set; }

        public class RegisterInput
        {
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            public string PhoneNum { get; set; } = string.Empty;
            public string? Email { get; set; }
            public int Role { get; set; } = 4;
        }

        // Server-side registration proxy
        public async Task<IActionResult> OnPostServerRegisterAsync()
        {
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
            var content = await resp.Content.ReadFromJsonAsync<object>();
            if (!resp.IsSuccessStatusCode)
            {
                Message = $"Register failed: {resp.StatusCode}";
                return Page();
            }

            Message = "Registration request sent (server-side).";
            return Page();
        }
    }
}
