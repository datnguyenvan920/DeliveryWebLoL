using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Client_Frontend.Pages;

public class LogoutModel : PageModel
{
    public IActionResult OnPost()
    {
        // Frontend currently doesn't maintain an auth cookie/session.
        // This endpoint exists to support a logout navigation flow.
        // If you later persist tokens (cookie/session/localStorage), clear them here.
        return RedirectToPage("/Login");
    }

    public IActionResult OnGet()
    {
        // Allow GET for convenience during development.
        return RedirectToPage("/Login");
    }
}
