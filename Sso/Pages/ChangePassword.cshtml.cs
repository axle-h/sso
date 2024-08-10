using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Sso.Identity;

namespace Sso.Pages;

[BindProperties]
public class ChangePassword(UserManager<SsoUser> userManager) : PageModel
{
    [Required] public string NewPassword { get; set; } = string.Empty;
    [Required] public string ConfirmPassword { get; set; } = string.Empty;

    public IActionResult OnGet() => CleanPage();

    public async Task<IActionResult> OnPost()
    {
        if (!ModelState.IsValid)
        {
            return CleanPage();
        }

        if (NewPassword != ConfirmPassword)
        {
            ModelState.AddModelError(string.Empty, "Them passwords don't match");
            return CleanPage();
        }
        
        var user = await userManager.GetUserAsync(HttpContext.User);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Server error");
            return CleanPage();
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, NewPassword);
        if (result.Succeeded)
        {
            // TODO toast somehow
            return Redirect("~/");
        }

        ModelState.AddModelError(string.Empty, result.GetPasswordError() ?? "That password is not good"); 

        return CleanPage();
    }
    
    private PageResult CleanPage()
    {
        NewPassword = string.Empty;
        ConfirmPassword = string.Empty;
        return Page();
    }
}