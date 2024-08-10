using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sso.Identity;

namespace Sso.Pages.Admin;

[Authorize(Roles = "admin")]
public class UpdateUser(SsoUserManager userManager, RoleManager<IdentityRole> roleManager) : PageModel
{
    public SsoUser? CurrentUser { get; set; }
    
    [BindProperty]
    public UpdateUserModel? Model { get; set; }
    
    public List<RoleOption> RoleOptions { get; set; } = [];
    
    public Task<IActionResult> OnGet(string username) => InitPage(username);

    public async Task<IActionResult> OnPost(string username)
    {
        if (Model is null || !ModelState.IsValid)
        {
            return await InitPage(username);
        }
        
        CurrentUser = await userManager.FindByNameAsync(username);
        if (CurrentUser is null)
        {
            return NotFound();
        }

        if (Model.Password is not null)
        {
            var changePassword = await userManager.ChangePasswordUnsafeAsync(CurrentUser, Model.Password);
            var passwordError = changePassword.GetPasswordError();
            if (passwordError is not null)
            {
                ModelState.AddModelError("Model.Password", passwordError);
                return await InitPage(username);
            }
        }

        var userRoles = (await userManager.GetRolesAsync(CurrentUser)).ToHashSet();
        
        var addToRoles = await userManager.AddToRolesAsync(CurrentUser, Model.Roles.Except(userRoles));
        if (!addToRoles.Succeeded)
        {
            ModelState.AddModelError("Model.Roles", addToRoles.GetGenericError());
        }

        var rolesToRemove = userRoles.Except(Model.Roles).ToHashSet();
        if (CurrentUser.UserName == User.Identity!.Name)
        {
            // cannot de-admin yourself
            rolesToRemove.Remove("admin");
        }
        
        var removeRoles = await userManager.RemoveFromRolesAsync(CurrentUser, rolesToRemove);
        if (!removeRoles.Succeeded)
        {
            ModelState.AddModelError("Model.Roles", removeRoles.GetGenericError());
        }
        

        CurrentUser.FirstName = Model.FirstName;
        CurrentUser.LastName = Model.LastName;
        // also updates the user...
        var updatedUser = await userManager.UpdateSecurityStampAsync(CurrentUser);
        if (!updatedUser.Succeeded)
        {
            ModelState.AddModelError("", updatedUser.GetGenericError());
            return await InitPage(username);
        }
        
        // TODO toast somehow
        return Redirect("/Admin/ListUsers");
    }

    private async Task<IActionResult> InitPage(string username)
    {
        CurrentUser ??= await userManager.FindByNameAsync(username);
        if (CurrentUser is null)
        {
            return NotFound();
        }

        Model ??= new UpdateUserModel
        {
            FirstName = CurrentUser.FirstName,
            LastName = CurrentUser.LastName,
            Roles = (await userManager.GetRolesAsync(CurrentUser)).ToHashSet()
        };
        Model.Password = null;
        
        RoleOptions = await roleManager.Roles
            .OrderBy(r => r.Name)
            .Where(r => r.Name != null)
            .Select(r => new RoleOption(r.Name!, Model.Roles.Contains(r.Name!)))
            .ToListAsync();
        
        return Page();
    }
}

[BindProperties]
public class UpdateUserModel
{
    public string? Password { get; set; }
    
    [Required]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    public string LastName { get; set; } = string.Empty;
    
    public HashSet<string> Roles { get; set; } = [];
}