using Duende.IdentityServer;
using Duende.IdentityServer.Events;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Test;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Sso.Pages.Login;

[AllowAnonymous]
public class Index(
    IIdentityServerInteractionService interaction,
    IEventService events,
    TestUserStore users
) : PageModel {

    [BindProperty]
    public InputModel Input { get; set; } = default!;

    public IActionResult OnGet(string? returnUrl)
    {
        BuildModel(returnUrl);
        return Page();
    }
        
    public async Task<IActionResult> OnPost()
    {
        // check if we are in the context of an authorization request
        var context = await interaction.GetAuthorizationContextAsync(Input.ReturnUrl);

        if (ModelState.IsValid)
        {
            // validate username/password against in-memory store
            if (users.ValidateCredentials(Input.Username, Input.Password))
            {
                var user = users.FindByUsername(Input.Username);
                await events.RaiseAsync(new UserLoginSuccessEvent(user.Username, user.SubjectId, user.Username, clientId: context?.Client.ClientId));

                // only set explicit expiration here if user chooses "remember me". 
                // otherwise we rely upon expiration configured in cookie middleware.
                var props = new AuthenticationProperties();
                // if (Input.RememberLogin)
                // {
                //     props.IsPersistent = true;
                //     props.ExpiresUtc = DateTimeOffset.UtcNow.Add(TimeSpan.FromDays(30));
                // };

                // issue authentication cookie with subject ID and username
                var issuer = new IdentityServerUser(user.SubjectId)
                {
                    DisplayName = user.Username
                };

                await HttpContext.SignInAsync(issuer, props);

                if (context != null)
                {
                    return Redirect(Input.ReturnUrl ?? "~/");
                }

                // request for a local page
                if (Url.IsLocalUrl(Input.ReturnUrl))
                {
                    return Redirect(Input.ReturnUrl);
                }

                if (string.IsNullOrEmpty(Input.ReturnUrl))
                {
                    return Redirect("~/");
                }

                // user might have clicked on a malicious link - should be logged
                throw new ArgumentException("invalid return URL");
            }

            await events.RaiseAsync(new UserLoginFailureEvent(Input.Username, "invalid credentials", clientId:context?.Client.ClientId));
            ModelState.AddModelError(string.Empty, "Invalid username or password");
        }

        // something went wrong, show form with error
        BuildModel(Input.ReturnUrl);
        return Page();
    }

    private void BuildModel(string? returnUrl)
    {
        Input = new InputModel
        {
            ReturnUrl = returnUrl
        };
    }
}
