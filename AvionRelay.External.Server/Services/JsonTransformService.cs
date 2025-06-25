using Microsoft.Extensions.Logging;

namespace AvionRelay.External.Server.Services;

/// <summary>
/// Service that handles JSON transformation based on client preferences
/// </summary>
public class JsonTransformService
{
    private readonly ConnectionTracker _connectionTracker;
    private readonly ILogger<JsonTransformService> _logger;
    private const string JsonCaseMetadataKey = "JsonCase";
    
    public JsonTransformService(ConnectionTracker connectionTracker, ILogger<JsonTransformService> logger)
    {
        _connectionTracker = connectionTracker;
        _logger = logger;
    }
    
    /// <summary>
    /// Transforms a list of JsonResponse objects for a specific client
    /// </summary>
    public List<ResponsePayload> TransformResponsesForClient(string clientId, List<ResponsePayload> responses)
    {
        var caseType = GetClientCasePreference(clientId);
        if (caseType == JsonCaseType.None)
        {
            return responses;
        }
        try
        {
            return responses.Select(resp =>
            {
                if (resp.ResponseJson is not null)
                {
                    string updatedJson = JsonExtensions.TransformJsonKeys(resp.ResponseJson, caseType);
                    return new ResponsePayload(resp.MessageId,resp.Receiver, resp.HandledAt,updatedJson);
                }

                return resp;
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transforming responses for client {ClientId}", clientId);
            return responses; // Return original on error
        }
    }
    
    /// <summary>
    /// Transforms JSON based on the client's preferred case type
    /// </summary>
    /// <param name="clientId">The client ID (or transport ID)</param>
    /// <param name="json">The JSON to transform</param>
    /// <returns>Transformed JSON or original if no transformation needed</returns>
    public string TransformForClient(string clientId, string json)
    {
        try
        {
            var caseType = GetClientCasePreference(clientId);
            if (caseType == JsonCaseType.None)
            {
                return json;
            }

            var transformed = JsonExtensions.TransformJsonKeys(json, caseType);
            _logger.LogDebug("Transformed JSON keys to {CaseType} for client {ClientId}", caseType, clientId);
            return transformed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transforming JSON for client {ClientId}", clientId);
            return json; // Return original on error
        }
    }
    
    /// <summary>
    /// Transforms a TransportPackage for a specific client
    /// </summary>
    public TransportPackage TransformPackageForClient(string clientId, TransportPackage package)
    {
        var caseType = GetClientCasePreference(clientId);
        if (caseType == JsonCaseType.None)
        {
            return package;
        }

        try
        {
            var transformedJson = JsonExtensions.TransformJsonKeys(package.MessageJson, caseType);
            
            // Create a new package with transformed JSON
            return package with { MessageJson = transformedJson };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transforming package for client {ClientId}", clientId);
            return package; // Return original on error
        }
    }
    
   
    
    /// <summary>
    /// Checks if a client has a case preference
    /// </summary>
    public bool ClientHasCasePreference(string clientId)
    {
        return GetClientCasePreference(clientId) != JsonCaseType.None;
    }
    
    /// <summary>
    /// Gets the case preference for a client by checking both connection and registration metadata
    /// </summary>
    private JsonCaseType GetClientCasePreference(string clientOrTransportId)
    {
        // Use the improved lookup method (assuming it's added to ConnectionTracker)
        var connection = _connectionTracker.GetConnection(clientOrTransportId);
        
        // If not found, try to resolve via ID mappings
        if (connection == null)
        {
            var clientId = _connectionTracker.GetClientIDFromTransportID(clientOrTransportId);
            if (!string.IsNullOrEmpty(clientId))
            {
                connection = _connectionTracker.GetConnection(clientId);
            }
            else
            {
                var transportId = _connectionTracker.GetTransportIDFromClientID(clientOrTransportId);
                if (!string.IsNullOrEmpty(transportId))
                {
                    connection = _connectionTracker.GetConnection(transportId);
                }
            }
        }
        
        if (connection == null)
        {
            _logger.LogDebug("No connection found for ID {ClientOrTransportId}", clientOrTransportId);
            return JsonCaseType.None;
        }
        
        // Check metadata for JsonCase preference
        if (connection.Metadata.TryGetValue(JsonCaseMetadataKey, out var caseValue))
        {
            var caseType = JsonExtensions.ParseCaseType(caseValue?.ToString());
            _logger.LogDebug("Client {ClientId} prefers {CaseType} JSON keys", connection.ClientName ?? clientOrTransportId, caseType);
            return caseType;
        }
        
        return JsonCaseType.None;
    }
}