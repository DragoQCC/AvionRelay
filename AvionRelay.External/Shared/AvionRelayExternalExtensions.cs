using HelpfulTypesAndExtensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace AvionRelay.External;

public class AvionRelayClientOptions
{
    public required string Name { get; set; }
    public required string ClientVersion { get; set; }
    public List<string> SupportedMessageNames { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

public static class AvionRelayExternalExtensions
{
    public static IServiceCollection AddAvionRelayExternalMessaging(this IServiceCollection services)
    {
        
        services.AddMessageTypeResolver();
        return services;
    }
    
    /// <summary>
    /// Starts the required SignalR connection to the Server in the background and monitors for connection issues to reestablish connections
    /// </summary>
    /// <param name="app"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public static IApplicationBuilder UseAvionRelayExternalMessaging(this IApplicationBuilder app, AvionRelayClientOptions options)
    {
        TransportPackageExtensions.Initialize(app.ApplicationServices);
        var externalMessageBus = app.ApplicationServices.GetRequiredService<AvionRelayExternalBus>();
        _ = Task.Run(async () =>
            {
                await externalMessageBus.StartAsync();
                externalMessageBus.AvionRelayClient = await externalMessageBus.RegisterClient(options.Name, options.ClientVersion, options.SupportedMessageNames, options.Metadata);
            }
        );
        return app;
    }

}