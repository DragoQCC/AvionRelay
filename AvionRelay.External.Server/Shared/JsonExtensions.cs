using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace AvionRelay.External.Server;

/// <summary>
/// Supported JSON key casing conventions
/// </summary>
public enum JsonCaseType
{
    /// <summary>
    /// No transformation (default)
    /// </summary>
    None,
    
    /// <summary>
    /// camelCase - firstName, lastName
    /// </summary>
    CamelCase,
    
    /// <summary>
    /// PascalCase - FirstName, LastName
    /// </summary>
    PascalCase,
    
    /// <summary>
    /// snake_case - first_name, last_name
    /// </summary>
    SnakeCase,
    
    /// <summary>
    /// kebab-case - first-name, last-name
    /// </summary>
    KebabCase,
    
    /// <summary>
    /// SCREAMING_SNAKE_CASE - FIRST_NAME, LAST_NAME
    /// </summary>
    ScreamingSnakeCase
}

public static class JsonExtensions
{
    private static readonly Regex CamelCaseRegex = new(@"([a-z])([A-Z])", RegexOptions.Compiled);
    private static readonly Regex SnakeCaseRegex = new(@"_([a-z])", RegexOptions.Compiled);
    private static readonly Regex KebabCaseRegex = new(@"-([a-z])", RegexOptions.Compiled);
    
    /// <summary>
    /// Attempts to extract the specified json key value and convert it into the specified type
    /// </summary>
    /// <param name="json"></param>
    /// <param name="keyName"></param>
    /// <param name="options"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T? TryGetJsonSubsectionAs<T>(string json, string keyName, JsonSerializerOptions? options = null)
    {
        //pull out just the metadata from the request
        var parsedJobj = JObject.Parse(json);
        if (parsedJobj.TryGetValue(keyName,StringComparison.InvariantCultureIgnoreCase, out JToken? jsonContent) is false)
        {
            return default;
        }
        string parsedSubsectionString = jsonContent.ToString();
            
        // Convert to internal types
        T? convertedType = JsonSerializer.Deserialize<T>(parsedSubsectionString,options);
        return convertedType;
    }

    /// <summary>
    /// Transforms JSON string keys to the specified case type
    /// </summary>
    public static string TransformJsonKeys(string json, JsonCaseType targetCase)
    {
        if (targetCase == JsonCaseType.None || string.IsNullOrWhiteSpace(json))
        {
            return json;
        }

        try
        {
            var jsonNode = JsonNode.Parse(json);
            if (jsonNode == null)
            {
                return json;
            }

            TransformNode(jsonNode, targetCase);
            return jsonNode.ToJsonString();
        }
        catch (Exception)
        {
            // If parsing fails, return original JSON
            return json;
        }
    }
    
    /// <summary>
    /// Transforms JSON object keys to the specified case type
    /// </summary>
    public static JsonObject TransformJsonObject(JsonObject jsonObject, JsonCaseType targetCase)
    {
        if (targetCase == JsonCaseType.None)
        {
            return jsonObject;
        }

        var transformed = new JsonObject();
        
        foreach (var property in jsonObject)
        {
            var transformedKey = TransformKey(property.Key, targetCase);
            var value = property.Value?.DeepClone();
            
            if (value != null)
            {
                TransformNode(value, targetCase);
            }
            
            transformed[transformedKey] = value;
        }
        
        return transformed;
    }
    
    private static void TransformNode(JsonNode node, JsonCaseType targetCase)
    {
        switch (node)
        {
            case JsonObject obj:
                var properties = obj.ToList();
                obj.Clear();
                
                foreach (var property in properties)
                {
                    var transformedKey = TransformKey(property.Key, targetCase);
                    var value = property.Value;
                    
                    if (value != null)
                    {
                        TransformNode(value, targetCase);
                    }
                    
                    obj[transformedKey] = value;
                }
                break;
                
            case JsonArray array:
                foreach (var item in array)
                {
                    if (item != null)
                    {
                        TransformNode(item, targetCase);
                    }
                }
                break;
        }
    }
    
    /// <summary>
    /// Transforms a single key to the specified case type
    /// </summary>
    public static string TransformKey(string key, JsonCaseType targetCase)
    {
        if (string.IsNullOrEmpty(key) || targetCase == JsonCaseType.None)
        {
            return key;
        }

        // First, normalize the key by detecting its current format
        var words = SplitIntoWords(key);
        
        return targetCase switch
        {
            JsonCaseType.CamelCase => ToCamelCase(words),
            JsonCaseType.PascalCase => ToPascalCase(words),
            JsonCaseType.SnakeCase => ToSnakeCase(words),
            JsonCaseType.KebabCase => ToKebabCase(words),
            JsonCaseType.ScreamingSnakeCase => ToScreamingSnakeCase(words),
            _ => key
        };
    }
    
    private static List<string> SplitIntoWords(string input)
    {
        var words = new List<string>();
        
        // Handle snake_case and kebab-case
        input = input.Replace('_', ' ').Replace('-', ' ');
        
        // Handle camelCase and PascalCase
        input = CamelCaseRegex.Replace(input, "$1 $2");
        
        // Split by spaces and filter empty entries
        return input.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w.ToLowerInvariant())
            .ToList();
    }
    
    private static string ToCamelCase(List<string> words)
    {
        if (words.Count == 0)
        {
            return "";
        }

        return words[0] + string.Concat(words.Skip(1).Select(Capitalize));
    }
    
    private static string ToPascalCase(List<string> words)
    {
        return string.Concat(words.Select(Capitalize));
    }
    
    private static string ToSnakeCase(List<string> words)
    {
        return string.Join("_", words);
    }
    
    private static string ToKebabCase(List<string> words)
    {
        return string.Join("-", words);
    }
    
    private static string ToScreamingSnakeCase(List<string> words)
    {
        return string.Join("_", words.Select(w => w.ToUpperInvariant()));
    }
    
    private static string Capitalize(string word)
    {
        if (string.IsNullOrEmpty(word))
        {
            return word;
        }

        return char.ToUpperInvariant(word[0]) + word.Substring(1);
    }
    
    /// <summary>
    /// Parses JsonCaseType from string (case-insensitive)
    /// </summary>
    public static JsonCaseType ParseCaseType(string? caseType)
    {
        if (string.IsNullOrWhiteSpace(caseType))
        {
            return JsonCaseType.None;
        }

        return caseType.ToLowerInvariant() switch
        {
            "camelcase" or "camel_case" => JsonCaseType.CamelCase,
            "pascalcase" or "pascal_case" => JsonCaseType.PascalCase,
            "snakecase" or "snake_case" => JsonCaseType.SnakeCase,
            "kebabcase" or "kebab_case" or "kebab-case" => JsonCaseType.KebabCase,
            "screamingsnakecase" or "screaming_snake_case" => JsonCaseType.ScreamingSnakeCase,
            _ => JsonCaseType.None
        };
    }
    
}