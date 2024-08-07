using Duende.IdentityServer.EntityFramework.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Sso.Identity;

namespace Sso.Migrations;

public class MigrationOptions
{
    public bool Migrate { get; set; } = false;
}

public class MigrationService(
    IServiceProvider provider,
    IOptions<MigrationOptions> options,
    ILogger<MigrationService> logger
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!options.Value.Migrate)
        {
            return;
        }

        await using var scope = provider.CreateAsyncScope();

        List<DbContext> contexts =
        [
            scope.ServiceProvider.GetRequiredService<SsoDbContext>(),
            scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>(),
            scope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>()
        ];

        foreach (var context in contexts)
        {
            logger.LogInformation("migrating {Context}", context.GetType().Name);
            await context.Database.MigrateAsync(cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}