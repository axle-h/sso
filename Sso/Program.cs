using Microsoft.EntityFrameworkCore;
using Sso;
using Serilog;
using Serilog.Events;
using Sso.Migrations;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

builder.Services.AddRazorPages();

Action<DbContextOptionsBuilder> dbBuilder = b => b.UseSqlite(
    builder.Configuration.GetConnectionString("Db") ?? "Data Source=data.db",
    sql => sql.MigrationsAssembly(typeof(Program).Assembly.GetName().Name)
);

var isBuilder = builder.Services.AddIdentityServer(options =>
    {
        // TODO disable on ssl
        options.Authentication.CookieSameSiteMode = SameSiteMode.Lax;
        
        options.KeyManagement.Enabled = true;
                
        options.Events.RaiseErrorEvents = true;
        options.Events.RaiseInformationEvents = true;
        options.Events.RaiseFailureEvents = true;
        options.Events.RaiseSuccessEvents = true;

        // see https://docs.duendesoftware.com/identityserver/v6/fundamentals/resources/
        options.EmitStaticAudienceClaim = true;
    })
    .AddConfigurationStore(options =>
    {
        options.ConfigureDbContext = dbBuilder;
    })
    .AddOperationalStore(options =>
    {
        options.ConfigureDbContext = dbBuilder;
    })
    .AddTestUsers(TestUsers.Users);


// in-memory, code config
isBuilder.AddInMemoryIdentityResources(Config.IdentityResources);

builder.Services.AddAuthentication();


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
app.UseIdentityServer();
app.UseAuthorization();
        
app.MapRazorPages()
    .RequireAuthorization();

app.MigrateDatabase();

app.Run();