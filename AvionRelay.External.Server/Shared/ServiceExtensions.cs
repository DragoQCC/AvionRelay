using AvionRelay.Core;
using AvionRelay.Core.Services;
using AvionRelay.External.Server.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AvionRelay.External.Server;

public static class ServiceExtensions
{
    public static IServiceCollection AddAvionRelayServerServices(this IServiceCollection services, AvionRelayExternalOptions avionRelayConfiguration)
    {
        services.AddSingleton<MessagingManager>();
        services.AddSingleton<AvionRelayExternalOptions>(avionRelayConfiguration);
        services.AddSingleton<AvionRelayOptions>(avionRelayConfiguration);
        services.AddSingleton<IMessageScheduler, MessageSchedulerService>();
        services.AddSingleton<MessageStatistics>();
        services.AddSingleton<TransportMonitorAggregator>();
        services.AddSingleton<MessageHandlerTracker>();
        services.AddSingleton<ResponseTracker>();
        services.AddSingleton<AvionRelayTransportRouter>();
        services.AddSingleton<JsonTransformService>();


        switch (avionRelayConfiguration.StorageConfig.Provider)
        {
            case StorageProvider.SQLite :
                services.AddSingleton<SqliteDatabaseService>(provider => new SqliteDatabaseService(
                                                                 new SqliteOptions(), provider.GetRequiredService<ILogger<SqliteDatabaseService>>())
                );

                services.AddSingleton<IExternalMessageStorage, SqliteMessageStorage>();
                break;
            case StorageProvider.PostgreSQL :
                //TODO: Add PostgreSQL implementation
                break;
            case StorageProvider.InMemory :
            default : services.AddSingleton<IMessageStorage, InMemoryStorage>(); break;
        }

        return services;
    }

    public static IHostApplicationBuilder ConfigureAvionRelayPortBindings(this WebApplicationBuilder builder, AvionRelayExternalOptions avionRelayOptions)
    {
        // Configure Kestrel to listen on specific endpoints
        builder.WebHost.ConfigureKestrel((context, serverOptions) =>
            {
                // Default HTTPS endpoint for web UI and SignalR
                serverOptions.ListenAnyIP(7008, listenOptions => { listenOptions.UseHttps(); });

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
                        serverOptions.ListenAnyIP(
                            port, listenOptions =>
                            {
                                listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2;

                                if (avionRelayOptions.Transports.Grpc.EnableTls)
                                {
                                    listenOptions.UseHttps(); // You can configure certificate here if needed
                                }
                            }
                        );
                    }
                    else
                    {
                        serverOptions.Listen(
                            System.Net.IPAddress.Parse(host), port, listenOptions =>
                            {
                                listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2;

                                if (avionRelayOptions.Transports.Grpc.EnableTls)
                                {
                                    listenOptions.UseHttps();
                                }
                            }
                        );
                    }
                }
            }
        );
        return builder;
    }

}