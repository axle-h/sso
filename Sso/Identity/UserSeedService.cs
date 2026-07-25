using Microsoft.AspNetCore.Identity;
using Sso.Configuration;

namespace Sso.Identity;

/// <summary>
/// Reconciles the configured users and their roles into the database on startup.
/// Runs after <see cref="Sso.Migrations.MigrationService"/>, which is why registration order matters.
/// </summary>
public class UserSeedService(
    IServiceProvider provider,
    IConfiguration configuration,
    ILogger<UserSeedService> logger
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var users = configuration.GetSection(SsoUserOptions.SectionName).Get<List<SsoUserOptions>>() ?? [];
        if (users.Count == 0)
        {
            logger.LogWarning("no users configured, nobody will be able to log in");
            return;
        }

        await using var scope = provider.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<SsoUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        var roles = users
            .SelectMany(u => u.Roles)
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => r.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var role in roles)
        {
            if (await roleManager.RoleExistsAsync(role))
            {
                continue;
            }

            logger.LogInformation("creating role {Role}", role);
            Check(await roleManager.CreateAsync(new IdentityRole(role)), $"create role {role}");
        }

        foreach (var options in users)
        {
            await SeedUser(userManager, options);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task SeedUser(UserManager<SsoUser> userManager, SsoUserOptions options)
    {
        var username = options.Username?.Trim();
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new InvalidOperationException($"a configured user has no username, set {SsoUserOptions.SectionName}__n__Username");
        }

        var user = await userManager.FindByNameAsync(username);
        if (user is null)
        {
            if (string.IsNullOrWhiteSpace(options.Password))
            {
                throw new InvalidOperationException(
                    $"user '{username}' does not exist and has no password configured, " +
                    $"set {SsoUserOptions.SectionName}__n__Password to create them");
            }

            logger.LogInformation("creating user {Username}", username);
            user = new SsoUser
            {
                UserName = username,
                Email = options.Email,
                EmailConfirmed = true,
                FirstName = options.FirstName ?? string.Empty,
                LastName = options.LastName ?? string.Empty
            };
            Check(await userManager.CreateAsync(user, options.Password), $"create user {username}");
        }
        else if (SyncProfile(user, options))
        {
            logger.LogInformation("updating user {Username}", username);
            Check(await userManager.UpdateAsync(user), $"update user {username}");
        }

        await SyncRoles(userManager, user, username, options);
    }

    /// <summary>
    /// Config owns the profile. The password is deliberately left alone, it is whatever the user last set.
    /// </summary>
    private static bool SyncProfile(SsoUser user, SsoUserOptions options)
    {
        var changed = false;

        if (options.Email is not null && user.Email != options.Email)
        {
            user.Email = options.Email;
            user.EmailConfirmed = true;
            changed = true;
        }

        if (options.FirstName is not null && user.FirstName != options.FirstName)
        {
            user.FirstName = options.FirstName;
            changed = true;
        }

        if (options.LastName is not null && user.LastName != options.LastName)
        {
            user.LastName = options.LastName;
            changed = true;
        }

        return changed;
    }

    private async Task SyncRoles(UserManager<SsoUser> userManager, SsoUser user, string username, SsoUserOptions options)
    {
        var current = await userManager.GetRolesAsync(user);
        var desired = options.Roles.Select(r => r.Trim()).ToList();

        var missing = desired.Except(current, StringComparer.OrdinalIgnoreCase).ToList();
        if (missing.Count > 0)
        {
            logger.LogInformation("granting {Username} roles {Roles}", username, missing);
            Check(await userManager.AddToRolesAsync(user, missing), $"grant roles to {username}");
        }

        var extra = current.Except(desired, StringComparer.OrdinalIgnoreCase).ToList();
        if (extra.Count > 0)
        {
            logger.LogInformation("revoking {Username} roles {Roles}", username, extra);
            Check(await userManager.RemoveFromRolesAsync(user, extra), $"revoke roles from {username}");
        }
    }

    private static void Check(IdentityResult result, string what)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"failed to {what}: {result.GetGenericError()}");
        }
    }
}
