using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.AspNetCore.Identity;

namespace Sso.Identity;

public class SsoUser : IdentityUser
{
    [Required]
    [MaxLength(255)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string LastName { get; set; } = string.Empty;
}

public static class SsoUSerExtensions
{
    public static string Initials(this SsoUser user)
    {
        var result = new StringBuilder();

        if (user.FirstName.Length > 0)
        {
            result.Append(user.FirstName[0]);
        }
        
        if (user.LastName.Length > 0)
        {
            result.Append(user.LastName[0]);
        }

        return result.ToString().ToUpperInvariant();
    }
}