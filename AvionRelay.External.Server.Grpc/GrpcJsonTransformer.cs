using System.Text.Json;
using System.Text.Json.Nodes;
using AvionRelay.External.Transports.Grpc;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AvionRelay.External.Server.Grpc;

/// <summary>
/// Transforms JSON between C# and gRPC/protobuf formats
/// </summary>
public static class GrpcJsonTransformer
{
    /// <summary>
    /// Transforms a message JSON string to be compatible with gRPC/protobuf clients
    /// Converts ISO 8601 date strings to protobuf Timestamp format
    /// </summary>
    public static string TransformForGrpcClient(string messageJson)
    {
        try
        {
            /*Core.Messages.MessageContext context = messageJson.GetMessageContextFromJson();
            
            var grpcContext = new External.Transports.Grpc.MessageContext
            {
                CreatedAt = Timestamp.FromDateTime(DateTime.SpecifyKind(context.CreatedAt.DateTime, DateTimeKind.Utc)),
                
                
            };*/

            var jsonNode = JsonNode.Parse(messageJson);
            if (jsonNode == null) return messageJson;

            // Transform metadata timestamps
            var metadata = jsonNode["metadata"];
            if (metadata != null)
            {
                // Convert CreatedAt
                if (metadata["createdAt"] != null)
                {
                    var createdAtStr = metadata["createdAt"]?.GetValue<string>();
                    if (!string.IsNullOrEmpty(createdAtStr) && DateTime.TryParse(createdAtStr, out var createdAt))
                    {
                        //todo: this is currently giving a value like "2025-06-08T22:28:26.2760857\u002B00:00"
                        metadata["createdAt"] = ConvertToProtobufTimestamp(createdAt);
                    }
                }

                // Convert acknowledgements timestamps
                var acknowledgements = metadata["acknowledgements"]?.AsArray();
                if (acknowledgements != null)
                {
                    foreach (var ack in acknowledgements)
                    {
                        if (ack?["acknowledgedAt"] != null)
                        {
                            var ackTimeStr = ack["acknowledgedAt"]?.GetValue<string>();
                            if (!string.IsNullOrEmpty(ackTimeStr) && DateTime.TryParse(ackTimeStr, out var ackTime))
                            {
                                ack["acknowledgedAt"] = ConvertToProtobufTimestamp(ackTime);
                            }
                        }
                    }
                }

                // Convert MessageId from GUID string to lowercase (Python expects lowercase)
                if (metadata["messageId"] != null)
                {
                    var messageId = metadata["messageId"]?.GetValue<string>();
                    if (!string.IsNullOrEmpty(messageId))
                    {
                        metadata["messageId"] = messageId.ToLowerInvariant();
                    }
                }

                /*// Ensure numeric enums (not strings)
                ConvertEnumToNumber(metadata, "state");
                ConvertEnumToNumber(metadata, "priority");
                ConvertEnumToNumber(metadata, "baseMessageType");*/
            }
            Console.WriteLine($"Transformed JSON: {jsonNode.ToJsonString()}");

            return jsonNode.ToJsonString();
        }
        catch (Exception ex)
        {
            // If transformation fails, return original
            // Log the error in production
            Console.WriteLine($"Failed to transform JSON for gRPC: {ex.Message}");
            return messageJson;
        }
    }
    
    /// <summary>
    /// Transforms a message JSON string from gRPC/protobuf format back to C# format
    /// </summary>
    public static string TransformFromGrpcClient(string messageJson)
    {
        try
        {
            var jsonNode = JsonNode.Parse(messageJson);
            if (jsonNode == null) return messageJson;
            
            // Transform metadata timestamps back to ISO 8601
            var metadata = jsonNode["metadata"];
            if (metadata != null)
            {
                // Convert CreatedAt
                if (metadata["createdAt"]?.AsObject() != null)
                {
                    var timestamp = metadata["createdAt"];
                    if (timestamp?["seconds"] != null)
                    {
                        var dateTime = ConvertFromProtobufTimestamp(timestamp);
                        metadata["createdAt"] = dateTime.ToString("O"); // ISO 8601
                    }
                }
                
                // Convert acknowledgements timestamps
                var acknowledgements = metadata["acknowledgements"]?.AsArray();
                if (acknowledgements != null)
                {
                    foreach (var ack in acknowledgements)
                    {
                        if (ack?["acknowledgedAt"]?.AsObject() != null)
                        {
                            var timestamp = ack["acknowledgedAt"];
                            if (timestamp?["seconds"] != null)
                            {
                                var dateTime = ConvertFromProtobufTimestamp(timestamp);
                                ack["acknowledgedAt"] = dateTime.ToString("O");
                            }
                        }
                    }
                }
            }
            
            return jsonNode.ToJsonString();
        }
        catch (Exception ex)
        {
            // If transformation fails, return original
            Console.WriteLine($"Failed to transform JSON from gRPC: {ex.Message}");
            return messageJson;
        }
    }
    
    private static JsonNode ConvertToProtobufTimestamp(DateTime dateTime)
    {
        var timestamp = Timestamp.FromDateTime(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc));
        return JsonNode.Parse(JsonFormatter.Default.Format(timestamp))!;
    }
    
    private static DateTime ConvertFromProtobufTimestamp(JsonNode timestampNode)
    {
        var seconds = timestampNode["seconds"]?.GetValue<long>() ?? 0;
        var nanos = timestampNode["nanos"]?.GetValue<int>() ?? 0;
        
        var timestamp = new Timestamp { Seconds = seconds, Nanos = nanos };
        return timestamp.ToDateTime();
    }
    
    private static void ConvertEnumToNumber(JsonNode node, string propertyName)
    {
        if (node?[propertyName] != null)
        {
            var value = node[propertyName];
            if (value != null && value.GetValueKind() == JsonValueKind.String)
            {
                // Try to parse as enum name
                var stringValue = value.GetValue<string>();
                if (int.TryParse(stringValue, out var numValue))
                {
                    node[propertyName] = numValue;
                }
                else
                {
                    // Convert known enum names to numbers
                    node[propertyName] = ConvertEnumNameToValue(propertyName, stringValue);
                }
            }
        }
    }
    
    private static int ConvertEnumNameToValue(string enumType, string enumName)
    {
        return enumType switch
        {
            "state" => enumName?.ToUpperInvariant() switch
            {
                "CREATED" => 0,
                "SENT" => 1,
                "RECEIVED" => 2,
                "PROCESSING" => 3,
                "RESPONDED" => 4,
                "RESPONSECEIVED" => 100,
                "ACKNOWLEDGEMENTRECEIVED" => 101,
                "FAILED" => 200,
                _ => 0
            },
            "priority" => enumName?.ToUpperInvariant() switch
            {
                "LOWEST" => 0,
                "LOW" => 1,
                "NORMAL" => 2,
                "HIGH" => 3,
                "VERYHIGH" => 4,
                "HIGHEST" => 5,
                _ => 2
            },
            "baseMessageType" => enumName?.ToUpperInvariant() switch
            {
                "COMMAND" => 0,
                "NOTIFICATION" => 1,
                "ALERT" => 2,
                "INSPECTION" => 3,
                _ => 0
            },
            _ => 0
        };
    }
}