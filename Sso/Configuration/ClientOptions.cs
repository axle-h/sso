using Duende.IdentityModel;
using Duende.IdentityServer.Models;

namespace Sso.Configuration;

/// <summary>
/// An OIDC client, bound from the "Clients" section keyed by client id.
/// Every client is a next-auth app of the same shape, so only the origins are configured.
/// </summary>
public class ClientOptions
{
    /// <summary>
    /// Display name, shown on the logged out page. Defaults to the client id.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Shared secret. Never commit this, it comes from the environment
    /// e.g. Clients__risk__Secret from a kubernetes secret.
    /// </summary>
    public string? Secret { get; set; }

    /// <summary>
    /// Origins the client is served from, the first of which receives front channel logout.
    /// Add e.g. http://localhost:3000 to develop a client against this instance.
    /// </summary>
    public List<string> Origins { get; set; } = [];

    public List<string> Scopes { get; set; } = [];
}

public static class ClientConfiguration
{
    public const string SectionName = "Clients";

    private const string CallbackPath = "/api/auth/callback/axh-sso";
    private const string LogoutPath = "/logout";

    public static IReadOnlyList<Client> ToClients(this IDictionary<string, ClientOptions> options)
    {
        if (options.Count == 0)
        {
            throw new InvalidOperationException(
                $"no clients configured, expected at least one entry under the {SectionName} section");
        }

        return options.Select(kv => ToClient(kv.Key, kv.Value)).ToList();
    }

    private static Client ToClient(string clientId, ClientOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Secret))
        {
            throw new InvalidOperationException(
                $"client '{clientId}' has no secret, set {SectionName}__{clientId}__Secret");
        }

        var origins = options.Origins
            .Where(o => !string.IsNullOrWhiteSpace(o))
            .Select(o => o.Trim().TrimEnd('/'))
            .ToList();

        if (origins.Count == 0)
        {
            throw new InvalidOperationException(
                $"client '{clientId}' has no origins, set {SectionName}__{clientId}__Origins__0");
        }

        if (options.Scopes.Count == 0)
        {
            throw new InvalidOperationException(
                $"client '{clientId}' has no scopes, set {SectionName}__{clientId}__Scopes__0");
        }

        // everything not set here is a Duende default and matches what the old
        // configuration store was seeded with
        return new Client
        {
            ClientId = clientId,
            ClientName = options.Name ?? clientId,
            ClientSecrets = [new Secret(options.Secret.Sha256())],
            AllowedGrantTypes = GrantTypes.CodeAndClientCredentials,
            AllowedScopes = options.Scopes,
            RedirectUris = origins.Select(o => o + CallbackPath).ToList(),
            PostLogoutRedirectUris = origins.Select(o => o + "/").ToList(),
            FrontChannelLogoutUri = origins[0] + LogoutPath,
            AllowOfflineAccess = true
        };
    }
}
