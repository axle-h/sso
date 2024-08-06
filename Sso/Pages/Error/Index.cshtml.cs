using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Sso.Pages.Error;

[AllowAnonymous]
public class Index(IIdentityServerInteractionService interaction) : PageModel
{
    public ErrorMessage? Error { get; set; }

    public async Task OnGet(string? errorId)
    {
        Error = await interaction.GetErrorContextAsync(errorId);
    }
}
