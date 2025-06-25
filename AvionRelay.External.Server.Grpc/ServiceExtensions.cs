using AvionRelay.External.Server.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace AvionRelay.External.Server.Grpc;

public static class ServiceExtensions
{
    /// <summary>
    /// Adds gRPC hub support for the SERVER application
    /// </summary>
    public static IServiceCollection AddAvionRelayGrpcHub(this IServiceCollection services, GrpcOptions? options = null)
    {
        options ??= new();
       
        // Add gRPC
        services.AddGrpc(grpcOptions =>
        {
            grpcOptions.MaxReceiveMessageSize = options.MaxMessageSize;
            grpcOptions.MaxSendMessageSize = options.MaxMessageSize;
            grpcOptions.EnableDetailedErrors = options.EnableDetailedErrors;
        });
        
        if (options.EnableReflection)
        {
            services.AddGrpcReflection();
        }
        
        //add the grpc transport to DI
        services.AddSingleton<IAvionRelayTransport,AvionRelayGrpcTransport>();
        // Add gRPC monitoring components
        services.AddSingleton<GrpcTransportMonitor>();
        services.AddSingleton<ITransportMonitor>(sp => sp.GetRequiredService<GrpcTransportMonitor>());
        
        return services;
    }
    
    /// <summary>
    /// Maps the gRPC service endpoint
    /// </summary>
    public static IEndpointRouteBuilder MapAvionRelayGrpcService(this IEndpointRouteBuilder endpoints, AvionRelayExternalOptions avionRelayOptions)
    {
        if (avionRelayOptions.EnabledTransports.Contains(TransportTypes.Grpc))
        {
            var options = avionRelayOptions.Transports.Grpc;
            endpoints.MapGrpcService<AvionRelayGrpcTransport>();
            if (options.EnableReflection)
            {
                endpoints.MapGrpcReflectionService();
            }
        }
        return endpoints;
    }
}