namespace Sso.Configuration;

/// <summary>
/// A user, bound from the "Users" section. There is no admin UI, config is the source of truth
/// for who exists and what roles they hold.
/// </summary>
public class UserOptions
{
    public const string SectionName = "Users";

    public string? Username { get; set; }

    public string? Email { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    /// <summary>
    /// Only used to create the user. Once they exist their password is theirs to change,
    /// editing this will not reset it. Never commit this, it comes from the environment
    /// e.g. Users__0__Password from a kubernetes secret.
    /// </summary>
    public string? Password { get; set; }

    public List<string> Roles { get; set; } = [];
}
