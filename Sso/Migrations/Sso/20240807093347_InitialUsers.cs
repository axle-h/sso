using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sso.Migrations.Sso;

/// <inheritdoc />
public partial class InitialUsers : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        var factory = new DataFactory(migrationBuilder);
        factory.Role("afba81f5-a308-4fdf-a8e0-32e0e4755363", "make-money");
        factory.Role("47e94660-dfcf-42c1-9e2e-1b496ce55b2c", "make-movies");
        factory.Role("407a0482-fb56-42a8-a34b-1cb320d668c1", "risk");
        factory.Role("980277c0-22d0-4dc1-affc-5e5d98fefe40", "admin");
        
        factory.User(
            "310a5cab-8d15-4d45-b178-a3b1be01c3b2",
            "alex",
            "password",
            "Alex",
            "Haslehurst",
            "alex.haslehurst@gmail.com",
            "make-money", "make-movies", "risk", "admin"
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DeleteData("Users", "Id", "310a5cab-8d15-4d45-b178-a3b1be01c3b2");
        migrationBuilder.DeleteData("Roles", "Id", "afba81f5-a308-4fdf-a8e0-32e0e4755363");
        migrationBuilder.DeleteData("Roles", "Id", "47e94660-dfcf-42c1-9e2e-1b496ce55b2c");
        migrationBuilder.DeleteData("Roles", "Id", "407a0482-fb56-42a8-a34b-1cb320d668c1");
        migrationBuilder.DeleteData("Roles", "Id", "980277c0-22d0-4dc1-affc-5e5d98fefe40");
    }
}

internal class DataFactory(MigrationBuilder migrationBuilder)
{
    private readonly UpperInvariantLookupNormalizer _normalizer = new();
    private readonly PasswordHasher<IdentityUser> _passwordHasher = new();

    private readonly Dictionary<string, string> _roles = [];
    
    public void Role(string id, string name)
    {
        _roles[name] = id;
        migrationBuilder.InsertData(
            "Roles",
            ["Id", "Name", "NormalizedName", "ConcurrencyStamp"],
            [id, name, _normalizer.NormalizeName(name), Guid.NewGuid().ToString()]
        );
    }
    
    public void User(
        string id, string username, string password,
        string firstName, string lastName, string email,
        params string[] roleNames)
    {
        migrationBuilder.InsertData(
            "Users",
            [
                "Id", "FirstName", "LastName",
                "UserName", "NormalizedUserName",
                "Email", "NormalizedEmail", "EmailConfirmed",
                "PasswordHash", "SecurityStamp", "ConcurrencyStamp",
                "PhoneNumber", "PhoneNumberConfirmed", "TwoFactorEnabled",
                "LockoutEnd", "LockoutEnabled", "AccessFailedCount"
            ],
            [
                id, firstName, lastName,
                username, _normalizer.NormalizeName(username),
                email, _normalizer.NormalizeEmail(email), true,
                _passwordHasher.HashPassword(new IdentityUser(), password),
                Base32.GenerateBase32(), Guid.NewGuid().ToString(),
                null, false, false,
                null, true, 0
            ]
        );

        foreach (var roleName in roleNames)
        {
            var roleId = _roles[roleName] ?? throw new ArgumentException($"unknown role name {roleName}");
            migrationBuilder.InsertData("UserRoles", ["UserId", "RoleId"], [id, roleId]);
        }
    }

    
}

internal static class Base32
{
    private const string _base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
 
    public static string GenerateBase32()
    {
        const int length = 20;
        // base32 takes 5 bytes and converts them into 8 characters, which would be (byte length / 5) * 8
        // except that it also pads ('=') for the last processed chunk if it's less than 5 bytes.
        // So in order to handle the padding we add 1 less than the chunk size to our byte length
        // which will either be removed due to integer division truncation if the length was already a multiple of 5
        // or it will increase the divided length by 1 meaning that a 1-4 byte length chunk will be 1 instead of 0
        // so the padding is now included in our string length calculation
        return string.Create(((length + 4) / 5) * 8, 0, static (buffer, _) =>
        {
            Span<byte> bytes = stackalloc byte[length];
            RandomNumberGenerator.Fill(bytes);

            var index = 0;
            for (int offset = 0; offset < bytes.Length;)
            {
                byte a, b, c, d, e, f, g, h;
                int numCharsToOutput = GetNextGroup(bytes, ref offset, out a, out b, out c, out d, out e, out f, out g, out h);

                buffer[index + 7] = ((numCharsToOutput >= 8) ? _base32Chars[h] : '=');
                buffer[index + 6] = ((numCharsToOutput >= 7) ? _base32Chars[g] : '=');
                buffer[index + 5] = ((numCharsToOutput >= 6) ? _base32Chars[f] : '=');
                buffer[index + 4] = ((numCharsToOutput >= 5) ? _base32Chars[e] : '=');
                buffer[index + 3] = ((numCharsToOutput >= 4) ? _base32Chars[d] : '=');
                buffer[index + 2] = (numCharsToOutput >= 3) ? _base32Chars[c] : '=';
                buffer[index + 1] = (numCharsToOutput >= 2) ? _base32Chars[b] : '=';
                buffer[index] = (numCharsToOutput >= 1) ? _base32Chars[a] : '=';
                index += 8;
            }
        });
    }
    
    private static int GetNextGroup(Span<byte> input, ref int offset, out byte a, out byte b, out byte c, out byte d, out byte e, out byte f, out byte g, out byte h)
    {
        uint b1, b2, b3, b4, b5;

        int retVal;
        switch (input.Length - offset)
        {
            case 1: retVal = 2; break;
            case 2: retVal = 4; break;
            case 3: retVal = 5; break;
            case 4: retVal = 7; break;
            default: retVal = 8; break;
        }

        b1 = (offset < input.Length) ? input[offset++] : 0U;
        b2 = (offset < input.Length) ? input[offset++] : 0U;
        b3 = (offset < input.Length) ? input[offset++] : 0U;
        b4 = (offset < input.Length) ? input[offset++] : 0U;
        b5 = (offset < input.Length) ? input[offset++] : 0U;

        a = (byte)(b1 >> 3);
        b = (byte)(((b1 & 0x07) << 2) | (b2 >> 6));
        c = (byte)((b2 >> 1) & 0x1f);
        d = (byte)(((b2 & 0x01) << 4) | (b3 >> 4));
        e = (byte)(((b3 & 0x0f) << 1) | (b4 >> 7));
        f = (byte)((b4 >> 2) & 0x1f);
        g = (byte)(((b4 & 0x3) << 3) | (b5 >> 5));
        h = (byte)(b5 & 0x1f);

        return retVal;
    }
}