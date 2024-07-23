using Sso;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

builder.Services.AddRazorPages();

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
    .AddTestUsers(TestUsers.Users);


// in-memory, code config
isBuilder.AddInMemoryIdentityResources(Config.IdentityResources);
isBuilder.AddInMemoryApiScopes(Config.ApiScopes);
isBuilder.AddInMemoryClients(Config.Clients);

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
    
app.Run();