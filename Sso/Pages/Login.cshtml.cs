using System.ComponentModel.DataAnnotations;
using Duende.IdentityServer.Events;
using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Sso.Identity;

namespace Sso.Pages;

[AllowAnonymous]
[BindProperties]
public class Index(
    IIdentityServerInteractionService interaction,
    IEventService events,
    SignInManager<SsoUser> signInManager
) : PageModel {
    
    [Required]
    public string? Username { get; set; }
    
    [Required]
    public string? Password { get; set; }

    public string? ReturnUrl { get; set; }

    public IActionResult OnGet(string? returnUrl)
    {
        ReturnUrl = returnUrl;
        return CleanPage();
    }
        
    public async Task<IActionResult> OnPost()
    {
        // check if we are in the context of an authorization request
        var context = await interaction.GetAuthorizationContextAsync(ReturnUrl);

        if (!ModelState.IsValid)
        {
            return CleanPage();
        }

        var result = await signInManager.PasswordSignInAsync(
            Username!,
            Password!,
            isPersistent: false,
            lockoutOnFailure: true
        );

        if (result.Succeeded)
        {
            await events.RaiseAsync(new UserLoginSuccessEvent(Username, HttpContext.User.GetSubjectId(), HttpContext.User.GetDisplayName(), clientId: context?.Client.ClientId));
            return Redirect(Url.IsLocalUrl(ReturnUrl) ? ReturnUrl : "~/");
        }

        if (result.IsLockedOut)
        {
            await events.RaiseAsync(new UserLoginFailureEvent(Username, "account locked", clientId:context?.Client.ClientId));
            ModelState.AddModelError(string.Empty, "Account is locked, please try again later");
            return CleanPage();
        }
        
        await events.RaiseAsync(new UserLoginFailureEvent(Username, "invalid credentials", clientId:context?.Client.ClientId));
        ModelState.AddModelError(string.Empty, "Looks like that's the wrong username or password");
        return CleanPage();
    }

    private PageResult CleanPage()
    {
        Password = null;
        return Page();
    }
}
