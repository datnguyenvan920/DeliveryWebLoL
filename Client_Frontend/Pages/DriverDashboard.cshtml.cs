using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Client_Frontend.Pages
{
    public class DriverDashboardModel : PageModel
    {
        public string Message { get; set; } = "";
        public void OnGet() { }
    }
}
