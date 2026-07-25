using Duende.IdentityModel;
using Duende.IdentityServer.Models;

namespace Sso.Configuration;

/// <summary>
/// The scopes served by this instance. These never vary per environment so they are not configurable.
/// </summary>
public static class SsoResources
{
    public const string RolesScope = "roles";
    public const string ReadUsersScope = "read_users";

    public static IEnumerable<IdentityResource> Identity =>
    [
        new IdentityResources.OpenId(),
        new IdentityResources.Profile(),
        new IdentityResources.Email(),
        new IdentityResource(RolesScope, "Roles", [JwtClaimTypes.Role]) { Required = true }
    ];

    public static IEnumerable<ApiScope> Api =>
    [
        new ApiScope(ReadUsersScope, "List Users")
        {
            Description = "Grants access to the list users API"
        }
    ];
}
