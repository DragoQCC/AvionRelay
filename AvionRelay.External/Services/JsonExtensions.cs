using System.Diagnostics;
using System.Text.Json;
using Serilog;

namespace AvionRelay.External;

public static class JsonExtensions
{
    /// <summary>
    /// Converts an object (potentially a JsonElement) to the specified type
    /// </summary>
    public static T ConvertTo<T>(this object? obj, JsonSerializerOptions? options = null)
    {
        if (obj == null)
        {
            return default(T)!;
        }

        // If it's already the correct type
        if (obj is T typed)
        {
            return typed;
        }

        // If it's a JsonElement (most common with SignalR)
        if (obj is JsonElement jsonElement)
        {
            return jsonElement.Deserialize<T>(options)!;
        }
        
        // If it's a Newtonsoft JObject/JToken
        if (obj.GetType().FullName?.StartsWith("Newtonsoft.Json.Linq") == true)
        {
            // Use reflection to avoid hard dependency on Newtonsoft
            var toObjectMethod = obj.GetType().GetMethod("ToObject", new[] { typeof(Type) });
            if (toObjectMethod != null)
            {
                return (T)toObjectMethod.Invoke(obj, new object[] { typeof(T) })!;
            }
        }

        if (obj is string str)
        {
            return JsonSerializer.Deserialize<T>(str, options)!;
        }
        
        // Fallback: serialize then deserialize
        var json = JsonSerializer.Serialize(obj, options);
        return JsonSerializer.Deserialize<T>(json, options)!;
    }

    public static T ConvertToIgnoreCase<T>(this object? obj)
    {
        JsonSerializerOptions options = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true
        };
        return ConvertTo<T>(obj,options);
    }

    public static string ToJson<T>(this T obj, JsonSerializerOptions? options = null)
    {
        return JsonSerializer.Serialize(obj, options);
    }

    public static string ToJsonIgnoreCase<T>(this T obj)
    {
        JsonSerializerOptions options = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true
        };
        return JsonSerializer.Serialize(obj, options);
    }
    

    [Conditional("DEBUG")]
    public static void PrettyPrintJsonString(this string jsonString)
    {
        object deserializeObject = JsonSerializer.Deserialize(jsonString, typeof(object))!;
        string prettyPrint = JsonSerializer.Serialize(deserializeObject, new JsonSerializerOptions { WriteIndented = true });
        Log.Logger.Information(prettyPrint);
    }
    
    
}