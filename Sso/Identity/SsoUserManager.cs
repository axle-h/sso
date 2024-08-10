using System.Transactions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Sso.Identity;

public class SsoUserManager(
    IUserStore<SsoUser> store,
    IOptions<IdentityOptions> optionsAccessor,
    IPasswordHasher<SsoUser> passwordHasher,
    IEnumerable<IUserValidator<SsoUser>> userValidators,
    IEnumerable<IPasswordValidator<SsoUser>> passwordValidators,
    ILookupNormalizer keyNormalizer,
    IdentityErrorDescriber errors,
    IServiceProvider services,
    ILogger<SsoUserManager> logger)
    : UserManager<SsoUser>(store, optionsAccessor, passwordHasher, userValidators, passwordValidators, keyNormalizer,
        errors, services, logger)
{
    public async Task<IdentityResult> ChangePasswordUnsafeAsync(SsoUser user, string newPassword)
    {
        var identityResult = await ValidatePasswordAsync(user, newPassword);
        if (!identityResult.Succeeded)
            return identityResult;
        
        await GetPasswordStore().SetPasswordHashAsync(user, PasswordHasher.HashPassword(user, newPassword), CancellationToken);
        await UpdateSecurityStampAsync(user);
        return IdentityResult.Success;
    }

    // public Task<IdentityResult> UpdateUserUnsafeAsync(SsoUser user) => UpdateUserAsync(user);

    private IUserPasswordStore<SsoUser> GetPasswordStore() =>
        Store as IUserPasswordStore<SsoUser> ?? throw new NotSupportedException("store is not a password store");
}
