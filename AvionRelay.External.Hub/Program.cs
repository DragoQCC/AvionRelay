using AvionRelay.Core;
using AvionRelay.Core.Services;
using AvionRelay.External.Hub.Components.Account;
using AvionRelay.External.Hub.Components.Account.Shared.Models;
using AvionRelay.External.Hub.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using AvionRelay.External.Hub.Components;
using AvionRelay.External.Hub.Features.Transports;
using Microsoft.Extensions.Options;
using MudBlazor.Services;

namespace AvionRelay.External.Hub;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        //add logging
        builder.Logging.AddConsole().SetMinimumLevel(LogLevel.Debug);

        // Add services to the container.
        builder.Services.AddRazorComponents().AddInteractiveServerComponents();
        builder.Services.AddMudServices();

        builder.Services.AddCascadingAuthenticationState();
        builder.Services.AddScoped<IdentityUserAccessor>();
        builder.Services.AddScoped<IdentityRedirectManager>();
        builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

        builder.Services.AddAuthentication(options =>
                {
                    options.DefaultScheme = IdentityConstants.ApplicationScheme;
                    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
                }
            )
            .AddIdentityCookies();
        
        //Avion Relay Config
        //builder.Services.Configure<AvionRelayExternalOptions>(builder.Configuration.GetSection("AvionRelay"));
        // Add AvionRelay services from the library
        builder.Services.AddSingleton<MessagingManager>();
        
        // Configure transports based on what's enabled
        var enabledTransports = builder.Configuration.GetSection("AvionRelay:EnabledTransports").Get<string[]>();
        
        //get the AvionRelayOptions from the configuration
        var avionRelayOptions = builder.Configuration.GetRequiredSection("AvionRelay").Get<AvionRelayExternalOptions>();
        Console.WriteLine($"AvionRelayOptions:");
        Console.WriteLine($"\tKeep Alive Interval: {avionRelayOptions.Transports.SignalR.KeepAliveIntervalSeconds} seconds");
        Console.WriteLine($"\tClient Timeout: {avionRelayOptions.Transports.SignalR.ClientTimeoutSeconds} seconds");
        
        if (enabledTransports.Contains("SignalR"))
        {
            //add SignalR hub
            builder.Services.AddAvionRelaySignalRHub(opt =>
            {
                opt.EnableDetailedErrors = avionRelayOptions.Transports.SignalR.EnableDetailedErrors;
                opt.MaxMessageSize = avionRelayOptions.Transports.SignalR.MaxMessageSize;
                opt.ClientTimeoutSeconds = avionRelayOptions.Transports.SignalR.ClientTimeoutSeconds;
                opt.KeepAliveIntervalSeconds = avionRelayOptions.Transports.SignalR.KeepAliveIntervalSeconds;
            });
        }

        builder.Services.AddSingleton<TransportMonitorAggregator>();


        await ConfigureMessageStorageProvider(builder);
        

        
        // Identity Services
        builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
            .AddSignInManager()
            .AddDefaultTokenProviders();
        
        builder.Services.AddTransient<IUserStore<ApplicationUser>, AvionRelayUserStore>();
        builder.Services.AddTransient<IRoleStore<ApplicationRole>, AvionRelayRoleStore>();
        
        
        

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            
        }
        else
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseAntiforgery();

        app.MapStaticAssets();
        app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

        // Add additional endpoints required by the Identity /Account Razor components.
        app.MapAdditionalIdentityEndpoints();
        
        app.MapHub<AvionRelaySignalRHub>("/avionrelay", options =>
        {
            options.AllowStatefulReconnects = true;
        });
        
        await app.RunAsync();
    }

    public static async Task ConfigureMessageStorageProvider(WebApplicationBuilder builder)
    {
        // Configure storage based on enum
        var avionRelayOptions = builder.Configuration
            .GetSection("AvionRelay")
            .Get<AvionRelayOptions>() ?? new AvionRelayOptions();

        switch (avionRelayOptions.StorageConfig.Provider)
        {
            case StorageProvider.SQLite:
                builder.Services.AddSingleton<SqliteDatabaseService>(provider 
                    => new SqliteDatabaseService(new SqliteOptions(), provider.GetRequiredService<ILogger<SqliteDatabaseService>>()));
                builder.Services.AddSingleton<IMessageStorage, SqliteMessageStorage>();
                break;
        
            case StorageProvider.PostgreSQL:
                //TODO: Add PostgreSQL implementation
                break;
        
            case StorageProvider.InMemory:
            default:
                builder.Services.AddSingleton<IMessageStorage, InMemoryStorage>();
                break;
        }
    }
}