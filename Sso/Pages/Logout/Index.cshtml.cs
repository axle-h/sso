
using Duende.IdentityServer.Events;
using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Sso.Identity;

namespace Sso.Pages.Logout;

[AllowAnonymous]
public class Index(
    IIdentityServerInteractionService interaction,
    IEventService events,
    SignInManager<SsoUser> signInManager
) : PageModel
{
    public async Task<IActionResult> OnGet(string? logoutId)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return RedirectToPage("/");
        }
        
        // if there's no current logout context, we need to create one
        // this captures necessary info from the current logged in user
        // this can still return null if there is no context needed
        logoutId ??= await interaction.CreateLogoutContextAsync();

        await events.RaiseAsync(new UserLogoutSuccessEvent(User.GetSubjectId(), User.GetDisplayName()));
        
        await signInManager.SignOutAsync();

        return RedirectToPage("/Logout/LoggedOut", new { logoutId });
    }
}
