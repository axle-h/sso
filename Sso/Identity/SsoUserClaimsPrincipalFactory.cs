using System.Security.Claims;
using Duende.IdentityModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Sso.Identity;

public class SsoUserClaimsPrincipalFactory(SsoUserManager userManager, IOptions<IdentityOptions> optionsAccessor)
    : UserClaimsPrincipalFactory<SsoUser>(userManager, optionsAccessor)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(SsoUser user)
    {
        var id = await base.GenerateClaimsAsync(user);

        id.AddClaim(new Claim(JwtClaimTypes.GivenName, user.FirstName));
        id.AddClaim(new Claim(JwtClaimTypes.FamilyName, user.LastName));

        var roles = await UserManager.GetRolesAsync(user);
        foreach (var role in roles)
        {
            id.AddClaim(new Claim(JwtClaimTypes.Role, role));
        }
        
        return id;
    }
}