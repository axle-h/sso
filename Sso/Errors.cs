using Microsoft.AspNetCore.Identity;

namespace Sso;

public static class Errors
{
    public static string? GetPasswordError(this IdentityResult result)
    {
        var errorCodes = result.Errors.Select(e => e.Code).ToHashSet();
        if (errorCodes.Contains(nameof(IdentityErrorDescriber.PasswordTooShort)))
        {
            return "That password is too short";           
        }

        if (errorCodes.Contains(nameof(IdentityErrorDescriber.PasswordRequiresNonAlphanumeric)))
        {
            return "That password needs some special characters";           
        }

        if (errorCodes.Contains(nameof(IdentityErrorDescriber.PasswordRequiresDigit)))
        {
            return "That password needs some numbers";           
        }

        if (errorCodes.Contains(nameof(IdentityErrorDescriber.PasswordRequiresLower)))
        {
            return "That password needs some lower case letters";           
        }

        if (errorCodes.Contains(nameof(IdentityErrorDescriber.PasswordRequiresUpper)))
        {
            return "That password needs some upper case letters";           
        }

        if (errorCodes.Contains(nameof(IdentityErrorDescriber.PasswordRequiresUniqueChars)))
        {
            return "That password is too weak";
        }

        return null;
    }

    public static string? GetEmailError(this IdentityResult result)
    {
        var errorCodes = result.Errors.Select(e => e.Code).ToHashSet();
        if (errorCodes.Contains(nameof(IdentityErrorDescriber.InvalidEmail)))
        {
            return "That email is not valid";           
        }
        
        if (errorCodes.Contains(nameof(IdentityErrorDescriber.DuplicateEmail)))
        {
            return "That email is already taken";           
        }

        return null;
    }
    
    public static string? GetUsernameError(this IdentityResult result)
    {
        var errorCodes = result.Errors.Select(e => e.Code).ToHashSet();
        if (errorCodes.Contains(nameof(IdentityErrorDescriber.InvalidUserName)))
        {
            return "That username is not valid";           
        }
        
        if (errorCodes.Contains(nameof(IdentityErrorDescriber.DuplicateUserName)))
        {
            return "That username is already taken";           
        }

        return null;
    }
}