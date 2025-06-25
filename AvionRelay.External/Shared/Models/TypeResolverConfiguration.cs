using AvionRelay.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AvionRelay.External;

public static class TypeResolverConfiguration
{
    public static IServiceCollection AddMessageTypeResolver(this IServiceCollection services)
    {
        // Register the type resolver as a singleton
        services.AddSingleton<ITypeResolver, MessageTypeResolver>();
        
        // Register the deserialization service
        services.AddSingleton<MessageDeserializationService>();
        
        return services;
    }
}