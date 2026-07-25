using Duende.IdentityServer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using Sso.Configuration;
using Sso.Identity;
using Sso.Migrations;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

builder.Services.AddHealthChecks();

builder.Services.AddRazorPages();
builder.Services.AddControllers();

var dbUrl = builder.Configuration.GetConnectionString("Db") ??
            throw new InvalidOperationException("Db connection string is required");

Action<DbContextOptionsBuilder> dbBuilder = b =>
{
    b.UseSqlite(dbUrl, sql =>
        sql.MigrationsAssembly(typeof(Program).Assembly.GetName().Name)
    );
};

builder.Services.AddDbContext<SsoDbContext>(dbBuilder);
builder.Services
    .AddIdentity<SsoUser, IdentityRole>(options =>
    {
        
        options.Password.RequiredLength = 10;
        options.Password.RequiredUniqueChars = 5;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireDigit = false;
        options.Password.RequireNonAlphanumeric = false;

        options.User.RequireUniqueEmail = true;

        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        options.Lockout.MaxFailedAccessAttempts = 5;
    })
    .AddEntityFrameworkStores<SsoDbContext>()
    .AddClaimsPrincipalFactory<SsoUserClaimsPrincipalFactory>()
    .AddTokenProvider<DataProtectorTokenProvider<SsoUser>>(TokenOptions.DefaultProvider);

// clients live in config rather than the database, there are only a handful of them and
// they change about as often as this app is deployed
var clientOptions = builder.Configuration.GetSection(ClientConfiguration.SectionName)
    .Get<Dictionary<string, ClientOptions>>() ?? new Dictionary<string, ClientOptions>();
var clients = clientOptions.ToClients();

var issuerUri = builder.Configuration.GetConnectionString("IssuerUri");
builder.Services.AddIdentityServer(options =>
    {
        options.IssuerUri = issuerUri;

        options.UserInteraction.LoginUrl = "/Login";
        options.UserInteraction.LogoutUrl = "/Logout";
        options.UserInteraction.ErrorUrl = "/Error";

        options.KeyManagement.Enabled = true;

        options.Events.RaiseErrorEvents = true;
        options.Events.RaiseInformationEvents = true;
        options.Events.RaiseFailureEvents = true;
        options.Events.RaiseSuccessEvents = true;

        // see https://docs.duendesoftware.com/identityserver/v6/fundamentals/resources/
        options.EmitStaticAudienceClaim = true;
    })
    .AddInMemoryIdentityResources(SsoResources.Identity)
    .AddInMemoryApiScopes(SsoResources.Api)
    .AddInMemoryClients(clients)
    // grants and signing keys are still persisted, dropping them would sign everyone
    // out of every client on each restart
    .AddOperationalStore(options => options.ConfigureDbContext = dbBuilder)
    .AddAspNetIdentity<SsoUser>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.AccessDeniedPath = "/AccessDenied";
    options.LoginPath = "/Login";
    options.LogoutPath = "/Logout";
    options.ReturnUrlParameter = "returnUrl";
    
    options.Cookie.Name = "sso.ax-h";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    
    options.ExpireTimeSpan = TimeSpan.FromMinutes(720);
    options.SlidingExpiration = true;
});

// Add authentication for the local API
builder.Services.AddAuthentication().AddLocalApi();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(SsoResources.ReadUsersScope, policy =>
    {
        policy.AddAuthenticationSchemes(IdentityServerConstants.LocalApi.AuthenticationScheme);
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("scope", SsoResources.ReadUsersScope);
    });
});

builder.Services
    .AddHostedService<MigrationService>()
    .AddOptions<MigrationOptions>()
    .BindConfiguration("Migration");

// must be registered after the migration service, hosted services start in registration order
builder.Services.AddHostedService<UserSeedService>();

var app = builder.Build();

app.UseSerilogRequestLogging(options =>
{
    options.GetLevel = (context, duration, exception) =>
    {
        if (context.Response.StatusCode > 499 || exception is not null)
        {
            // error
            return LogEventLevel.Error;
        }

        if (duration > 5000)
        {
            // slow request
            return LogEventLevel.Warning;
        }

        return LogEventLevel.Debug;
    };
});
    
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseStaticFiles();
app.UseRouting();
app.UseStatusCodePagesWithReExecute("/Error/Status/{0}");

app.UseIdentityServer();
app.UseAuthorization();

app.MapControllers();

app.MapRazorPages()
    .RequireAuthorization();

app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });

app.Run();