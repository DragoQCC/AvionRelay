using System.Collections.Concurrent;
using System.Reflection;
using AvionRelay.Core.Messages;
using Microsoft.Extensions.Logging;

namespace AvionRelay.Core.Services;

public interface ITypeResolver
{
    string? GetAssemblyQualifiedTypeName(string shortTypeName);
    Type? GetType(string shortTypeName);
    IEnumerable<Type> GetAllMessageTypes();
}

public class MessageTypeResolver : ITypeResolver
{
    private readonly ILogger<MessageTypeResolver>? _logger;
    private readonly ConcurrentDictionary<string, Type> _typeCache = new();

    // Namespaces that indicate gRPC/protobuf generated code
    private readonly string[] _grpcNamespaceIndicators = 
    {
        ".Grpc",
        ".Proto",
        "_pb2",
        "Google.Protobuf",
        "Grpc.Core"
    };

    // Preferred namespace patterns (higher priority)
    private readonly string[] _preferredNamespaces = 
    {
        "AvionRelay.Examples",
        "AvionRelay.Core.Messages",
        ".Messages",
        ".Commands",
        ".Alerts",
        ".Notifications",
        ".Inspections"
    };

    public MessageTypeResolver(ILogger<MessageTypeResolver>? logger = null)
    {
        _logger = logger;
        InitializeTypeCache();
    }

    /// <summary>
    /// Gets the assembly qualified type name for a given short type name.
    /// Prioritizes .NET types over gRPC-generated types.
    /// </summary>
    /// <param name="shortTypeName">The short name of the type (e.g., "GetStatusCommand")</param>
    /// <returns>The assembly qualified type name, or null if not found</returns>
    public string? GetAssemblyQualifiedTypeName(string shortTypeName)
    {
        var type = GetType(shortTypeName);
        return type?.AssemblyQualifiedName;
    }

    /// <summary>
    /// Gets the Type for a given short type name.
    /// </summary>
    /// <param name="shortTypeName">The short name of the type</param>
    /// <returns>The Type, or null if not found</returns>
    public Type? GetType(string shortTypeName)
    {
        if (string.IsNullOrWhiteSpace(shortTypeName))
        {
            return null;
        }

        // Check cache first
        if (_typeCache.TryGetValue(shortTypeName, out var cachedType))
        {
            _logger?.LogDebug("Type {TypeName} found in cache: {AssemblyQualifiedName}",  shortTypeName, cachedType.AssemblyQualifiedName);
            return cachedType;
        }

        // Search for the type
        var type = FindType(shortTypeName);
            
        if (type != null)
        {
            _typeCache[shortTypeName] = type;
            _logger?.LogInformation("Type {TypeName} resolved to: {AssemblyQualifiedName}", shortTypeName, type.AssemblyQualifiedName);
        }
        else
        {
            _logger?.LogWarning("Type {TypeName} not found in any loaded assembly", shortTypeName);
        }

        return type;
        
    }

    /// <summary>
    /// Gets all message types found in loaded assemblies
    /// </summary>
    public IEnumerable<Type> GetAllMessageTypes()
    {
        return _typeCache.Values.ToList();
    }

    private void InitializeTypeCache()
    {
        _logger?.LogDebug("Initializing type cache...");

        var assemblies = GetSearchableAssemblies();
        var messageTypes = new List<Type>();

        foreach (var assembly in assemblies)
        {
            try
            {
                var types = assembly.GetTypes()
                    .Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericTypeDefinition)
                    .Where(t => IsMessageType(t));

                messageTypes.AddRange(types);
            }
            catch (ReflectionTypeLoadException ex)
            {
                _logger?.LogWarning(ex, "Failed to load types from assembly {AssemblyName}", assembly.FullName);
            }
        }

        // Group by short name and select the best match for each
        var groupedTypes = messageTypes.GroupBy(t => t.Name);
        
        foreach (var group in groupedTypes)
        {
            var bestType = SelectBestType(group);
            if (bestType != null)
            {
                _typeCache[group.Key] = bestType;
                _logger?.LogDebug("Cached type {TypeName}: {AssemblyQualifiedName}", group.Key, bestType.AssemblyQualifiedName);
            }
        }
        
        _logger?.LogInformation("Type cache initialized with {Count} message types", _typeCache.Count);
    }

    private Type? FindType(string shortTypeName)
    {
        var assemblies = GetSearchableAssemblies();
        var candidateTypes = new List<Type>();

        foreach (var assembly in assemblies)
        {
            try
            {
                // Direct lookup first
                var types = assembly.GetTypes()
                    .Where(t => t.Name.Equals(shortTypeName, StringComparison.OrdinalIgnoreCase))
                    .Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericTypeDefinition);

                candidateTypes.AddRange(types);
            }
            catch (ReflectionTypeLoadException ex)
            {
                _logger?.LogWarning(ex, "Failed to search types in assembly {AssemblyName}", assembly.FullName);
            }
        }

        if (!candidateTypes.Any())
        {
            _logger?.LogDebug("No types found with name {TypeName}", shortTypeName);
            return null;
        }

        // If we have multiple candidates, select the best one
        return SelectBestType(candidateTypes);
    }

    private Type? SelectBestType(IEnumerable<Type> candidateTypes)
    {
        var typeList = candidateTypes.ToList();
            
        if (!typeList.Any())
        {
            return null;
        }

        if (typeList.Count == 1)
        {
            return typeList[0];
        }

        _logger?.LogDebug("Multiple types found, selecting best match from {Count} candidates", typeList.Count);

        // Score each type
        var scoredTypes = typeList.Select(t => new
            {
                Type = t,
                Score = CalculateTypeScore(t)
            })
            .OrderByDescending(x => x.Score)
            .ToList();

        // Log the scoring results
        foreach (var scored in scoredTypes)
        {
            _logger?.LogDebug("Type {FullName} scored {Score}",scored.Type.FullName, scored.Score);
        }

        return scoredTypes.First().Type;
    }

    private int CalculateTypeScore(Type type)
    {
        int score = 0;
        var fullName = type.FullName ?? "";
        var namespaceName = type.Namespace ?? "";

        // Negative scores for gRPC/protobuf generated code
        if (_grpcNamespaceIndicators.Any(indicator => fullName.Contains(indicator, StringComparison.OrdinalIgnoreCase)))
        {
            score -= 100;
        }

        // Positive scores for preferred namespaces
        foreach (var preferred in _preferredNamespaces)
        {
            if (namespaceName.Contains(preferred, StringComparison.OrdinalIgnoreCase))
            {
                score += 50;
            }
        }

        // Bonus for being in AvionRelay namespace
        if (namespaceName.StartsWith("AvionRelay", StringComparison.OrdinalIgnoreCase))
        {
            score += 25;
        }

        // Penalty for being in a .dll with "Grpc" in the name
        if (type.Assembly.FullName?.Contains("Grpc", StringComparison.OrdinalIgnoreCase) ?? false)
        {
            score -= 50;
        }

        // Bonus for inheriting from known base types
        if (IsAvionRelayMessage(type))
        {
            score += 100;
        }

        // Bonus for being a record type (common in your examples)
        if (type.IsSealed && type.BaseType?.Name == "Object" && 
            type.GetMethod("<Clone>$") != null) // Record types have a compiler-generated Clone method
        {
            score += 10;
        }

        return score;
    }

    private bool IsMessageType(Type type)
    {
        // Check if it's an AvionRelay message
        if (IsAvionRelayMessage(type))
        {
            return true;
        }

        // Check if the name ends with common message suffixes
        var messageSuffixes = new[] { "Command", "Alert", "Notification", "Inspection" };
        return messageSuffixes.Any(suffix => type.Name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsAvionRelayMessage(Type type)
    {
        // Check inheritance chain for AvionRelayMessage
        var baseType = type.BaseType;
        while (baseType != null)
        {
            if (baseType.Name == nameof(AvionRelayMessage) || baseType.FullName?.Contains(typeof(AvionRelayMessage).FullName) == true)
            {
                return true;
            }
            baseType = baseType.BaseType;
        }

        // Check interfaces for message contracts
        var messageInterfaces = new[] { nameof(IAvionRelayMessage) };
        return type.GetInterfaces().Any(i =>  messageInterfaces.Any(mi => i.Name.Contains(mi, StringComparison.OrdinalIgnoreCase)));
    }

    private IEnumerable<Assembly> GetSearchableAssemblies()
    {
        // Get all loaded assemblies
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic) // Skip dynamic assemblies
            .Where(a => !IsSystemAssembly(a))
            .ToList();

        // Also include referenced assemblies that might not be loaded yet
        var additionalAssemblies = new List<Assembly>();
        foreach (var assembly in assemblies.ToList())
        {
            try
            {
                var referencedAssemblies = assembly.GetReferencedAssemblies()
                    .Where(name => !IsSystemAssemblyName(name.Name ?? ""))
                    .Where(name => name.Name?.Contains("AvionRelay") == true ||
                               name.Name?.Contains("Example") == true);

                foreach (var referencedName in referencedAssemblies)
                {
                    try
                    {
                        var referencedAssembly = Assembly.Load(referencedName);
                        if (!assemblies.Contains(referencedAssembly))
                        {
                            additionalAssemblies.Add(referencedAssembly);
                        }
                    }
                    catch
                    {
                        // Ignore assemblies that can't be loaded
                    }
                }
            }
            catch
            {
                // Ignore errors getting referenced assemblies
            }
        }

        assemblies.AddRange(additionalAssemblies);
            
        _logger?.LogDebug("Searching {Count} assemblies for message types", assemblies.Count);
        return assemblies.Distinct();
    }

    private bool IsSystemAssembly(Assembly assembly)
    {
        var name = assembly.FullName ?? "";
        return IsSystemAssemblyName(name);
    }

    private bool IsSystemAssemblyName(string assemblyName)
    {
        var systemPrefixes = new[]
        {
            "System.",
            "Microsoft.",
            "mscorlib",
            "netstandard",
            "WindowsBase",
            "PresentationCore",
            "PresentationFramework"
        };

        return systemPrefixes.Any(prefix => 
                                      assemblyName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }
}

// Extension methods for easier use
public static class TypeResolverExtensions
{
    /// <summary>
    /// Creates an instance of a message type from its short name
    /// </summary>
    public static object? CreateMessageInstance(this ITypeResolver resolver, string shortTypeName)
    {
        var type = resolver.GetType(shortTypeName);
        if (type == null)
        {
            return null;
        }

        try
        {
            return Activator.CreateInstance(type);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Checks if a type with the given short name exists
    /// </summary>
    public static bool TypeExists(this ITypeResolver resolver, string shortTypeName)
    {
        return resolver.GetType(shortTypeName) != null;
    }
}