using Duende.IdentityServer;
using Duende.IdentityServer.Models;
using Microsoft.EntityFrameworkCore.Migrations;
using Serilog;
using ILogger = Serilog.ILogger;

#nullable disable

namespace Sso.Migrations.Configuration;

/// <inheritdoc />
public partial class InitialClients : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        var factory = new DataFactory(migrationBuilder);
            
        factory.Client("make-money", "Make Money", GrantTypes.CodeAndClientCredentials, ["openid", "profile", "email", "roles"])
            .RandomSecret()
            .RedirectUri("https://money.ax-h.com/api/auth/callback/axh-sso")
            .FrontChannelLogoutUri("https://money.ax-h.com/logout-oidc")
            .PostLogoutRedirectUri("https://money.ax-h.com/logout-oidc")
            .AllowOfflineAccess();
            
        factory.Client("make-movies", "Make Movies", GrantTypes.CodeAndClientCredentials, ["openid", "profile", "email", "roles"])
            .RandomSecret()
            .RedirectUri("https://movies.ax-h.com/api/auth/callback/axh-sso")
            .FrontChannelLogoutUri("https://movies.ax-h.com/logout-oidc")
            .PostLogoutRedirectUri("https://movies.ax-h.com/logout-oidc")
            .AllowOfflineAccess();
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // this should cascade
        migrationBuilder.DeleteData("Clients", "ClientId", "make-money");
        migrationBuilder.DeleteData("Clients", "ClientId", "make-movies");
    }
}

internal class DataFactory(MigrationBuilder migrationBuilder)
{
    private int _nextClientPk = 1;
    
    public ClientBuilder Client(string clientId, string name, IEnumerable<string> grants, IEnumerable<string> scopes)
    {
        var id = _nextClientPk++;
        migrationBuilder.InsertData(
            "Clients",
            [
                "Id", "ClientId", "ClientName", "Enabled", "ProtocolType",
                "RequireClientSecret", "RequireConsent", "AllowRememberConsent", "RequirePkce", "AllowPlainTextPkce",
                "RequireRequestObject", "AllowAccessTokensViaBrowser",
                "RequireDPoP", "DPoPValidationMode", "DPoPClockSkew",
                "FrontChannelLogoutSessionRequired", "BackChannelLogoutSessionRequired", "AllowOfflineAccess", "AlwaysIncludeUserClaimsInIdToken",
                "IdentityTokenLifetime", "AccessTokenLifetime", "AuthorizationCodeLifetime",
                "AbsoluteRefreshTokenLifetime", "SlidingRefreshTokenLifetime",
                "RequirePushedAuthorization", "RefreshTokenUsage", "UpdateAccessTokenClaimsOnRefresh",
                "RefreshTokenExpiration", "AccessTokenType", "EnableLocalLogin", "IncludeJwtId",
                "AlwaysSendClientClaims", "ClientClaimsPrefix", "DeviceCodeLifetime",
                "Created", "NonEditable"
            ],
            [
                id, clientId, name, true, IdentityServerConstants.ProtocolTypes.OpenIdConnect,
                true, false, true, true, false,
                false, false,
                false, (int) DPoPTokenExpirationValidationMode.Iat, TimeSpan.FromMinutes(5),
                true, true, false, false,
                300, 3600, 300,
                2592000, 1296000,
                false, (int) TokenUsage.ReUse, false,
                (int) TokenExpiration.Absolute, (int) AccessTokenType.Jwt, true, true,
                false, "_client", 300,
                DateTime.UtcNow, false
            ]
        );
        
        foreach (var grant in grants)
        {
            migrationBuilder.InsertData("ClientGrantTypes", ["ClientId", "GrantType"], [id, grant]);
        }
        
        foreach (var scope in scopes)
        {
            migrationBuilder.InsertData("ClientScopes", ["ClientId", "Scope"], [id, scope]);
        }
        
        return new ClientBuilder(migrationBuilder, id, clientId);
    }
}

internal class ClientBuilder(MigrationBuilder migrationBuilder, int id, string name)
{
    private readonly ILogger _logger = Log.ForContext(typeof(ClientBuilder));
        
    public ClientBuilder RandomSecret()
    {
        var secret = Guid.NewGuid().ToString().ToUpperInvariant();
        var secretValue = secret.Sha256();
        _logger.Information("client: {Name}, secret: {Secret}", name, secret);
        migrationBuilder.InsertData(
            "ClientSecrets",
            ["ClientId", "Type", "Created", "Value"],
            [id, IdentityServerConstants.SecretTypes.SharedSecret, DateTime.UtcNow, secretValue]
        );
        return this;
    }
        
    public ClientBuilder RedirectUri(string uri)
    {
        migrationBuilder.InsertData("ClientRedirectUris", ["ClientId", "RedirectUri"], [id, uri]);
        return this;
    }
        
    public ClientBuilder FrontChannelLogoutUri(string uri)
    {
        migrationBuilder.UpdateData("Clients", "Id", id, "FrontChannelLogoutUri", uri);
        return this;
    }

    public ClientBuilder PostLogoutRedirectUri(string uri)
    {
        migrationBuilder.InsertData("ClientPostLogoutRedirectUris", ["PostLogoutRedirectUri", "ClientId"], [uri, id]);
        return this;
    }
        
    public ClientBuilder AllowOfflineAccess()
    {
        migrationBuilder.UpdateData("Clients", "Id", id, "AllowOfflineAccess", true);
        return this;
    }
}

