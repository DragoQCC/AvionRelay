using System.Text.Json;
using AvionRelay.Core.Messages;
using Newtonsoft.Json.Linq;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace AvionRelay.External.Server;

public static class JsonExtensions
{
    public static MessageContext GetMessageContextFromJson(string json)
    {
        //pull out just the metadata from the request
        var parsedJobj = JObject.Parse(json);
        if (parsedJobj.TryGetValue("metadata",StringComparison.InvariantCultureIgnoreCase, out JToken? jsonContent) is false)
        {
            throw new Exception("Invalid message context");
        }

        string parsedMetadataString = jsonContent.ToString();
        JsonSerializerOptions options = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true
        };
            
        // Convert to internal types
        MessageContext messageMetadata = JsonSerializer.Deserialize<MessageContext>(parsedMetadataString,options);
        return messageMetadata;
    }
    
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
}