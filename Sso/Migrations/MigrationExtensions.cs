using Duende.IdentityServer.EntityFramework.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace Sso.Migrations;

public static class MigrationExtensions
{
    /// <summary>
    /// Migrates the DB for all contexts.
    /// Note: to add a new migration after each IdentityServer update:
    ///
    /// dotnet tool install --global dotnet-ef
    /// dotnet ef migrations add UpdateIdentityServer -c PersistedGrantDbContext -o Migrations/PersistedGrant
    /// dotnet ef migrations add UpdateIdentityServer -c ConfigurationDbContext -o Migrations/Configuration
    /// </summary>
    /// <param name="app"></param>
    public static void MigrateDatabase(this IApplicationBuilder app)
    {
        using var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>()!.CreateScope();
        serviceScope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>().Database.Migrate();
        serviceScope.ServiceProvider.GetRequiredService<ConfigurationDbContext>().Database.Migrate();
    }
}