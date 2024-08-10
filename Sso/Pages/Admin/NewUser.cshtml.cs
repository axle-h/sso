using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sso.Identity;

namespace Sso.Pages.Admin;

[Authorize(Roles = "admin")]
public class NewUser(UserManager<SsoUser> userManager, RoleManager<IdentityRole> roleManager) : PageModel
{

    [BindProperty]
    public NewUserModel Model { get; set; } = new();

    public List<RoleOption> RoleOptions { get; set; } = [];
    
    public Task<IActionResult> OnGet() => InitPage();

    public async Task<IActionResult> OnPost()
    {
        if (Model.Roles.Count == 0)
        {
            ModelState.AddModelError("Model.Roles", "At least one role is required");
        }
        
        if (!ModelState.IsValid)
        {
            return await InitPage();
        }

        var user = new SsoUser
        {
            UserName = Model.Username,
            Email = Model.Email,
            FirstName = Model.FirstName,
            LastName = Model.LastName
        };
        var createdUser = await userManager.CreateAsync(user, Model.Password);
        if (!createdUser.Succeeded)
        {
            var passwordError = createdUser.GetPasswordError();
            if (passwordError is not null)
            {
                ModelState.AddModelError("Model.Password", passwordError);
            }
            
            var emailError = createdUser.GetEmailError();
            if (emailError is not null)
            {
                ModelState.AddModelError("Model.Email", emailError);
            }
            
            var usernameError = createdUser.GetUsernameError();
            if (usernameError is not null)
            {
                ModelState.AddModelError("Model.Username", usernameError);
            }

            if (!ModelState.IsValid)
            {
                return await InitPage();
            }

            var identityErrors = createdUser.Errors.Select(e => e.Description);
            ModelState.AddModelError("", string.Join(", ", identityErrors));
            return await InitPage();
        }
        
        foreach (var role in Model.Roles)
        {
            var result = await userManager.AddToRoleAsync(user, role);
            if (!result.Succeeded)
            {
                ModelState.AddModelError("Model.Roles", $"Failed to add user to role {role}");
            }
        }
        
        // TODO toast somehow
        return Redirect("~/");
    }

    private async Task<IActionResult> InitPage()
    {
        RoleOptions = await roleManager.Roles
            .OrderBy(r => r.Name)
            .Where(r => r.Name != null)
            .Select(r => new RoleOption(r.Name!, Model.Roles.Contains(r.Name!)))
            .ToListAsync();
        Model.Password = string.Empty;
        return Page();
    }
}

public record RoleOption(string Name, bool Selected);

[BindProperties]
public class NewUserModel
{
    [Required]
    [RegularExpression("[a-z][a-z0-9_-]+", ErrorMessage = "Usernames must be lowercase letters and numbers")]
    public string Username { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    public string LastName { get; set; } = string.Empty;
    
    [Required]
    public string Password { get; set; } = string.Empty;
    
    public HashSet<string> Roles { get; set; } = [];
}