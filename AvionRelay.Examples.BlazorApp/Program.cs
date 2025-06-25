using AvionRelay.Examples.BlazorApp.Components;
using AvionRelay.External;
using AvionRelay.External.Transports.SignalR;
using MudBlazor.Services;

namespace AvionRelay.Examples.BlazorApp;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddMudServices();

        // Add services to the container.
        builder.Services.AddRazorComponents().AddInteractiveServerComponents();
        

        //add as a SignalR client
        builder.Services.AddAvionRelayExternalMessaging().WithSignalRMessageBus(opt => 
        {
            opt.HubUrl = "https://localhost:7008/avionrelay";
        });
        
        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }
        
        app.UseHttpsRedirection();
        app.UseAntiforgery();
        app.MapStaticAssets();
        app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
        
        //configure Avion relay external messaging
        AvionRelayClientOptions clientOptions = new AvionRelayClientOptions()
        {
            Name = "Example Blazor App",
            ClientVersion = "1.0.0",
            SupportedMessageNames = []
        };
        await app.UseAvionRelayExternalMessaging(clientOptions);

        await app.RunAsync();
    }
}