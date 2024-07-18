
using Duende.IdentityServer.Events;
using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Sso.Pages.Logout;

[AllowAnonymous]
public class Index(IIdentityServerInteractionService interaction, IEventService events) : PageModel
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

        // delete local authentication cookie
        await HttpContext.SignOutAsync();

        // raise the logout event
        await events.RaiseAsync(new UserLogoutSuccessEvent(User.GetSubjectId(), User.GetDisplayName()));

        return RedirectToPage("/Account/Logout/LoggedOut", new { logoutId });
    }
}
