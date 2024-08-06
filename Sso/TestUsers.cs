using IdentityModel;
using System.Security.Claims;
using System.Text.Json;
using Duende.IdentityServer;
using Duende.IdentityServer.Test;

namespace Sso;

public static class TestUsers
{

    private static string? Claim(this TestUser user, string type)
    {
        return user.Claims.FirstOrDefault(c => c.Type == type)?.Value;
    }
    
    public static string Initials(this TestUser user)
    {
        List<string?> names = [user.Claim(JwtClaimTypes.GivenName), user.Claim(JwtClaimTypes.FamilyName)];
        var initials = names.Where(x => !string.IsNullOrEmpty(x)).Select(x => x![0]);
        return string.Join("", initials);
    }
    
    public static string GivenName(this TestUser user)
    {
        return user.Claim(JwtClaimTypes.GivenName) ?? user.Username;
    }

    public static TestUser? TestUser(this ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity id) return null;
        var subjectId = id.FindFirst(JwtClaimTypes.Subject)?.Value;
        return subjectId is null ? null : Users.FirstOrDefault(u => u.SubjectId == subjectId);
    }
    
    public static List<TestUser> Users
    {
        get
        {
            var address = new
            {
                street_address = "One Hacker Way",
                locality = "Heidelberg",
                postal_code = "69118",
                country = "Germany"
            };
                
            return
            [
                new TestUser
                {
                    SubjectId = "1",
                    Username = "alice",
                    Password = "alice",
                    Claims =
                    {
                        new Claim(JwtClaimTypes.Name, "Alice Smith"),
                        new Claim(JwtClaimTypes.GivenName, "Alice"),
                        new Claim(JwtClaimTypes.FamilyName, "Smith"),
                        new Claim(JwtClaimTypes.Email, "AliceSmith@email.com"),
                        new Claim(JwtClaimTypes.EmailVerified, "true", ClaimValueTypes.Boolean),
                        new Claim(JwtClaimTypes.WebSite, "http://alice.com"),
                        new Claim(JwtClaimTypes.Address, JsonSerializer.Serialize(address),
                            IdentityServerConstants.ClaimValueTypes.Json)
                    }
                },

                new TestUser
                {
                    SubjectId = "2",
                    Username = "bob",
                    Password = "bob",
                    Claims =
                    {
                        new Claim(JwtClaimTypes.Name, "Bob Smith"),
                        new Claim(JwtClaimTypes.GivenName, "Bob"),
                        new Claim(JwtClaimTypes.FamilyName, "Smith"),
                        new Claim(JwtClaimTypes.Email, "BobSmith@email.com"),
                        new Claim(JwtClaimTypes.EmailVerified, "true", ClaimValueTypes.Boolean),
                        new Claim(JwtClaimTypes.WebSite, "http://bob.com"),
                        new Claim(JwtClaimTypes.Address, JsonSerializer.Serialize(address),
                            IdentityServerConstants.ClaimValueTypes.Json)
                    }
                }
            ];
        }
    }
}