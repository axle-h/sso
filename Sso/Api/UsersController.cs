using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sso.Identity;

namespace Sso.Api;

[Authorize("read_users")]
[Route("api/[controller]")]
[ApiController]
public class UsersController(UserManager<SsoUser> userManager) : ControllerBase
{
    [HttpGet]
    public async Task<List<User>> GetAll() => await userManager.Users
        .OrderBy(u => u.Id)
        .Select(user => new User(user.Id, user.UserName!, user.FirstName, user.LastName))
        .ToListAsync();
}

public record User(string Id, string Name, string FirstName, string LastName);