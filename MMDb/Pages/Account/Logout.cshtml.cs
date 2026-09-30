using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MMDb.Pages.Account;

/// <summary>
/// Signs out of MMDb, leaving the Pocket ID session in place.
/// </summary>
public class LogoutModel : PageModel
{
    /// <summary>
    /// Sends a direct visit back to the home page.
    /// </summary>
    public IActionResult OnGet()
    {
        return RedirectToPage("/Index");
    }

    /// <summary>
    /// Clears the sign-in cookie and returns to the home page.
    /// </summary>
    public async Task<IActionResult> OnPostAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToPage("/Index");
    }
}