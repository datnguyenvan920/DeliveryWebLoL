using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Client_Frontend.Pages
{
    public class ManagerDashboardModel : PageModel
    {
        public string Message { get; set; } = "";
        public void OnGet() { }
    }
}
