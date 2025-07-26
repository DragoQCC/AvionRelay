using AvionRelay.Examples.External.Hub.Components.Account;
using AvionRelay.Examples.External.Hub.Components.Account.Shared.Models;
using AvionRelay.External;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using AvionRelay.Examples.External.Hub.Components;
using AvionRelay.External.Server;
using AvionRelay.External.Server.Grpc;
using AvionRelay.External.Server.SignalR;
using HelpfulTypesAndExtensions;
using MudBlazor.Services;

namespace AvionRelay.Examples.External.Hub;

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

        // Identity Services
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
        
        builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
            .AddSignInManager()
            .AddDefaultTokenProviders();
        
        builder.Services.AddTransient<IUserStore<ApplicationUser>, AvionRelayUserStore>();
        builder.Services.AddTransient<IRoleStore<ApplicationRole>, AvionRelayRoleStore>();
        
        //configure Avion relay external messaging
        //get the AvionRelayOptions from the configuration
        AvionRelayExternalOptions avionRelayConfiguration =
            builder.Configuration.GetRequiredSection("AvionRelay").Get<AvionRelayExternalOptions>() ?? new AvionRelayExternalOptions();
        
        #if DEBUG
        Console.WriteLine("AvionRelayConfiguration");
        Console.WriteLine(avionRelayConfiguration.ToRecordLikeString());
        #endif
        
        builder.Services.AddAvionRelayServerServices(avionRelayConfiguration);
        builder.ConfigureAvionRelayPortBindings(avionRelayConfiguration);
        
        var enabledTransports = avionRelayConfiguration.EnabledTransports;
        
        if (enabledTransports.Contains(TransportTypes.SignalR))
        {
            builder.Services.AddAvionRelaySignalRHub(avionRelayConfiguration.Transports.SignalR);
        }
        if (enabledTransports.Contains(TransportTypes.Grpc))
        {
            builder.Services.AddAvionRelayGrpcHub(avionRelayConfiguration.Transports.Grpc);
        }

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
        app.UseAntiforgery();
        app.MapStaticAssets();
        app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
        app.MapAdditionalIdentityEndpoints();
        
        //configure Avion relay external messaging
        app.MapAvionRelaySignalRService(avionRelayConfiguration);
        app.MapAvionRelayGrpcService(avionRelayConfiguration);
        
        await app.RunAsync();
    }

    
}