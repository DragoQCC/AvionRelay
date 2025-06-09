using AvionRelay.Core;
using AvionRelay.Core.Services;
using AvionRelay.External.Hub.Components.Account;
using AvionRelay.External.Hub.Components.Account.Shared.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using AvionRelay.External.Hub.Components;
using AvionRelay.External.Server.Grpc;
using AvionRelay.External.Server.Services;
using AvionRelay.External.Server.SignalR;
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
        var avionRelayConfiguration = builder.Configuration.GetRequiredSection("AvionRelay").Get<AvionRelayExternalOptions>();
        
        await ConfigureWebBindings(builder, avionRelayConfiguration);
        
        if (enabledTransports.Contains("SignalR"))
        {
            //add SignalR hub
            builder.Services.AddAvionRelaySignalRHub(opt =>
            {
                opt.EnableDetailedErrors = avionRelayConfiguration.Transports.SignalR.EnableDetailedErrors;
                opt.MaxMessageSize = avionRelayConfiguration.Transports.SignalR.MaxMessageSize;
                opt.ClientTimeoutSeconds = avionRelayConfiguration.Transports.SignalR.ClientTimeoutSeconds;
                opt.KeepAliveIntervalSeconds = avionRelayConfiguration.Transports.SignalR.KeepAliveIntervalSeconds;
            });
        }

        if (enabledTransports.Contains("Grpc"))
        {
            builder.Services.AddAvionRelayGrpcHub(opt =>
            {
                opt.EnableDetailedErrors = avionRelayConfiguration.Transports.SignalR.EnableDetailedErrors;
                opt.EnableReflection = avionRelayConfiguration.Transports.Grpc.EnableReflection;
                opt.MaxMessageSize = avionRelayConfiguration.Transports.Grpc.MaxMessageSize;
            }
            );

            if (avionRelayConfiguration.Transports.Grpc.EnableReflection)
            {
                builder.Services.AddGrpcReflection();
            }
        }
        

        builder.Services.AddSingleton<MessageStatistics>();
        builder.Services.AddSingleton<TransportMonitorAggregator>();
        builder.Services.AddSingleton<MessageHandlerTracker>();
        builder.Services.AddSingleton<ResponseTracker>();
        builder.Services.AddSingleton<AvionRelayTransportRouter>();


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

        //app.UseHttpsRedirection();

        app.UseAntiforgery();

        app.MapStaticAssets();
        app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

        // Add additional endpoints required by the Identity /Account Razor components.
        app.MapAdditionalIdentityEndpoints();

        if (avionRelayConfiguration.EnabledTransports.Contains(TransportTypes.SignalR))
        {
            app.MapHub<AvionRelaySignalRTransport>(avionRelayConfiguration.Transports.SignalR.HubPath, options =>
            {
                options.AllowStatefulReconnects = true;
            });
        }

        if (avionRelayConfiguration.EnabledTransports.Contains(TransportTypes.Grpc))
        {
            app.MapAvionRelayGrpcService();
            if (avionRelayConfiguration.Transports.Grpc.EnableReflection)
            {
                app.MapGrpcReflectionService();
            }
        }
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

    public static async Task ConfigureWebBindings(WebApplicationBuilder builder, AvionRelayExternalOptions avionRelayOptions)
    {
        // Configure Kestrel to listen on specific endpoints
        builder.WebHost.ConfigureKestrel((context, serverOptions) =>
        {
            // Default HTTPS endpoint for web UI and SignalR
            serverOptions.ListenAnyIP(7008, listenOptions =>
            {
                listenOptions.UseHttps();
            });
            
            // Default HTTP endpoint
            serverOptions.ListenAnyIP(5172);
            
            // Dedicated gRPC endpoint
            if (avionRelayOptions.EnabledTransports.Contains(TransportTypes.Grpc))
            {
                var grpcAddress = avionRelayOptions.Transports.Grpc.ListenAddress;
                var parts = grpcAddress.Split(':');
                var host = parts[0];
                var port = int.Parse(parts[1]);
                
                if (host == "0.0.0.0")
                {
                    serverOptions.ListenAnyIP(port, listenOptions =>
                    {
                        listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2;
                        
                        if (avionRelayOptions.Transports.Grpc.EnableTls)
                        {
                            listenOptions.UseHttps(); // You can configure certificate here if needed
                        }
                    });
                }
                else
                {
                    serverOptions.Listen(System.Net.IPAddress.Parse(host), port, listenOptions =>
                    {
                        listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2;
                        
                        if (avionRelayOptions.Transports.Grpc.EnableTls)
                        {
                            listenOptions.UseHttps();
                        }
                    });
                }
            }
        });
    }
}