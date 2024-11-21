using Duende.IdentityServer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
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
    .AddUserManager<SsoUserManager>()
    .AddEntityFrameworkStores<SsoDbContext>()
    .AddClaimsPrincipalFactory<SsoUserClaimsPrincipalFactory>()
    .AddTokenProvider<DataProtectorTokenProvider<SsoUser>>(TokenOptions.DefaultProvider);

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
    .AddConfigurationStore(options => options.ConfigureDbContext = dbBuilder)
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
    options.AddPolicy("read_users", policy =>
    {
        policy.AddAuthenticationSchemes(IdentityServerConstants.LocalApi.AuthenticationScheme);
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("scope", "read_users");
    });
});

builder.Services
    .AddHostedService<MigrationService>()
    .AddOptions<MigrationOptions>()
    .BindConfiguration("Migration");

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