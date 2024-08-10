using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sso.Identity;

namespace Sso.Pages.Admin;

[Authorize(Roles = "admin")]
public class ListUsers(SsoUserManager userManager, ILogger<ListUsers> logger) : PageModel
{
    public List<SsoUser> Users { get; set; } = [];

    [BindProperty]
    public ActionModel Model { get; set; } = new();

    public Task OnGet() => InitPage();

    public async Task<IActionResult> OnPost()
    {
        if (!ModelState.IsValid)
        {
            return await InitPage();
        }

        if (Model.Action == UserActionType.Delete)
        {
            await DeleteUser();
        }

        return await InitPage();
    }

    private async Task DeleteUser()
    {
        if (Model.Username == User.Identity!.Name)
        {
            return;
        }
        
        logger.LogWarning("Deleting user {Username}", Model.Username);

        var user = await userManager.FindByNameAsync(Model.Username);
        if (user is null)
        {
            return;
        }

        await userManager.DeleteAsync(user);
    }

    private async Task<IActionResult> InitPage()
    {
        Users = await userManager.Users
            .OrderBy(u => u.UserName)
            .ToListAsync();
        return Page();
    }
}

[BindProperties]
public class ActionModel
{
    [Required] public string Username { get; set; } = string.Empty;

    [Required] public UserActionType? Action { get; set; }
}

public enum UserActionType
{
    Delete
}