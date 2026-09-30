using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MMDb.Pages.Account;

/// <summary>
/// Starts signing in with Pocket ID.
/// </summary>
public class LoginModel : PageModel
{
    /// <summary>
    /// Redirects to Pocket ID, returning to the given page afterwards.
    /// </summary>
    /// <param name="returnUrl">The local page to return to after signing in.</param>
    public IActionResult OnGet(string? returnUrl)
    {
        AuthenticationProperties properties = new() { RedirectUri = Url.IsLocalUrl(returnUrl) ? returnUrl : "/" };
        return Challenge(properties, OpenIdConnectDefaults.AuthenticationScheme);
    }
}