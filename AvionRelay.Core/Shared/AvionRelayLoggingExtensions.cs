using Microsoft.Extensions.Logging;

namespace AvionRelay.Core;

/// <summary>
/// Extension methods for logging AvionRelay events.
/// </summary>
public static class AvionRelayLoggingExtensions
{
    #region Information Logging Extensions
    
    // Message Lifecycle Information
    public static void LogMessageCreated(this ILogger logger, Guid messageId, string messageType)
        => logger.LogInformation(AvionRelayLogEvents.MessageCreated, "Message {MessageId} of type {MessageType} created", messageId, messageType);
    
    public static void LogMessageSent(this ILogger logger, Guid messageId, string messageType)
        => logger.LogInformation(AvionRelayLogEvents.MessageSent, "Message {MessageId} of type {MessageType} sent", messageId, messageType);
    
    public static void LogMessageReceived(this ILogger logger, Guid messageId, string messageType)
        => logger.LogInformation(AvionRelayLogEvents.MessageReceived, "Message {MessageId} of type {MessageType} received", messageId, messageType);
    
    public static void LogMessageAcknowledged(this ILogger logger, Guid messageId, string messageType)
        => logger.LogInformation(AvionRelayLogEvents.MessageAcknowledged, "Message {MessageId} of type {MessageType} acknowledged", messageId, messageType);
    
    public static void LogMessageResponded(this ILogger logger, Guid messageId, string messageType)
        => logger.LogInformation(AvionRelayLogEvents.MessageResponded, "Message {MessageId} of type {MessageType} responded to", messageId, messageType);
    
    public static void LogMessageResponseReceived(this ILogger logger, Guid messageId, string messageType)
        => logger.LogInformation(AvionRelayLogEvents.MessageResponseReceived, "Response received for message {MessageId} of type {MessageType}", messageId, messageType);
    
    public static void LogMessageCompleted(this ILogger logger, Guid messageId, string messageType)
        => logger.LogInformation(AvionRelayLogEvents.MessageCompleted, "Message {MessageId} of type {MessageType} completed", messageId, messageType);
    
    // System Information
    public static void LogSystemStarted(this ILogger logger)
        => logger.LogInformation(AvionRelayLogEvents.SystemStarted, "AvionRelay system started");
    
    public static void LogSystemStopped(this ILogger logger)
        => logger.LogInformation(AvionRelayLogEvents.SystemStopped, "AvionRelay system stopped");
    
    public static void LogProcessorRegistered(this ILogger logger, string processorType, string stateType)
        => logger.LogInformation(AvionRelayLogEvents.ProcessorRegistered, "Processor {ProcessorType} registered for state {StateType}", processorType, stateType);
    
    public static void LogHandlerRegistered(this ILogger logger, string handlerType, string messageType)
        => logger.LogInformation(AvionRelayLogEvents.HandlerRegistered, "Handler {HandlerType} registered for message type {MessageType}", handlerType, messageType);
    
    #endregion
    
    #region Debug Logging Extensions
    
    // Message Processing Debug
    public static void LogStateTransition(this ILogger logger, Guid messageId, string fromState, string toState)
        => logger.LogDebug(AvionRelayLogEvents.StateTransition, "Message {MessageId} transitioned from {FromState} to {ToState}", messageId, fromState, toState);
    
    public static void LogProcessorExecuted(this ILogger logger, Guid messageId, string processorType, long executionTimeMs)
        => logger.LogDebug(AvionRelayLogEvents.ProcessorExecuted, "Processor {ProcessorType} executed for message {MessageId} in {ExecutionTimeMs}ms", processorType, messageId, executionTimeMs);
    
    // Channel Debug
    public static void LogChannelStatistics(this ILogger logger, string channelName, int queueSize, int processedCount)
        => logger.LogDebug(AvionRelayLogEvents.ChannelStatistics, "Channel {ChannelName} statistics: Queue size: {QueueSize}, Processed: {ProcessedCount}", channelName, queueSize, processedCount);
    
    public static void LogPriorityQueueStatistics(this ILogger logger, int highPriorityCount, int normalPriorityCount, int lowPriorityCount)
        => logger.LogDebug(AvionRelayLogEvents.PriorityQueueStatistics, "Priority queue statistics: High: {HighPriorityCount}, Normal: {NormalPriorityCount}, Low: {LowPriorityCount}", highPriorityCount, normalPriorityCount, lowPriorityCount);
    
    #endregion
    
    #region Warning Logging Extensions
    
    // Message Processing Warnings
    public static void LogMessageProcessingDelayed(this ILogger logger, Guid messageId, string reason, long delayMs)
        => logger.LogWarning(AvionRelayLogEvents.MessageProcessingDelayed, "Processing of message {MessageId} delayed by {DelayMs}ms due to {Reason}", messageId, delayMs, reason);
    
    public static void LogMessageRetryAttempted(this ILogger logger, Guid messageId, int attemptNumber, int maxAttempts)
        => logger.LogWarning(AvionRelayLogEvents.MessageRetryAttempted, "Retry attempt {AttemptNumber}/{MaxAttempts} for message {MessageId}", attemptNumber, maxAttempts, messageId);
    
    public static void LogMessageTimeoutWarning(this ILogger logger, Guid messageId, string operation, long elapsedMs, long timeoutMs)
        => logger.LogWarning(AvionRelayLogEvents.MessageTimeoutWarning, "Operation {Operation} for message {MessageId} is taking longer than expected: {ElapsedMs}ms elapsed of {TimeoutMs}ms timeout", operation, messageId, elapsedMs, timeoutMs);
    
    public static void LogUnexpectedStateTransition(this ILogger logger, Guid messageId, string fromState, string toState)
        => logger.LogWarning(AvionRelayLogEvents.UnexpectedStateTransition, "Unexpected state transition for message {MessageId} from {FromState} to {ToState}", messageId, fromState, toState);
    
    // System Warnings
    public static void LogChannelBackpressure(this ILogger logger, string channelName, int queueSize, int threshold)
        => logger.LogWarning(AvionRelayLogEvents.ChannelBackpressure, "Channel {ChannelName} experiencing backpressure: Queue size {QueueSize} exceeds threshold {Threshold}", channelName, queueSize, threshold);
    
    public static void LogResourceUtilizationHigh(this ILogger logger, string resourceType, double utilization, double threshold)
        => logger.LogWarning(AvionRelayLogEvents.ResourceUtilizationHigh, "High utilization of {ResourceType}: {Utilization}% (threshold: {Threshold}%)", resourceType, utilization, threshold);
    
    #endregion
    
    #region Error Logging Extensions
    
    // Message Processing Errors
    public static void LogMessageProcessingFailed(this ILogger logger, Guid messageId, string messageType, Exception exception)
        => logger.LogError(AvionRelayLogEvents.MessageProcessingFailed, exception, "Processing failed for message {MessageId} of type {MessageType}", messageId, messageType);
    
    public static void LogMessageValidationFailed(this ILogger logger, Guid messageId, string messageType, string validationError)
        => logger.LogError(AvionRelayLogEvents.MessageValidationFailed, "Validation failed for message {MessageId} of type {MessageType}: {ValidationError}", messageId, messageType, validationError);
    
    public static void LogInvalidMessageState(this ILogger logger, Guid messageId, string currentState, string attemptedState)
        => logger.LogWarning(AvionRelayLogEvents.InvalidMessageState, "Invalid state for message {MessageId}: Current state is {CurrentState}, trying to set {attemptedState}", messageId, currentState, attemptedState);
    
    public static void LogMessageRetryLimitExceeded(this ILogger logger, Guid messageId, int maxAttempts)
        => logger.LogError(AvionRelayLogEvents.MessageRetryLimitExceeded, "Retry limit of {MaxAttempts} exceeded for message {MessageId}", maxAttempts, messageId);
    
    // Transport Errors
    public static void LogTransportSendFailed(this ILogger logger, Guid messageId, string transportType, Exception exception)
        => logger.LogError(AvionRelayLogEvents.TransportSendFailed, exception, "Failed to send message {MessageId} via transport {TransportType}", messageId, transportType);
    
    public static void LogTransportReceiveFailed(this ILogger logger, string transportType, Exception exception)
        => logger.LogError(AvionRelayLogEvents.TransportReceiveFailed, exception, "Failed to receive message via transport {TransportType}", transportType);
    
    // System Errors
    public static void LogProcessorExecutionFailed(this ILogger logger, Guid messageId, string processorType, Exception exception)
        => logger.LogError(AvionRelayLogEvents.ProcessorExecutionFailed, exception, "Processor {ProcessorType} failed while processing message {MessageId}", processorType, messageId);
    
    public static void LogChannelOperationFailed(this ILogger logger, string channelName, string operation, Exception exception)
        => logger.LogError(AvionRelayLogEvents.ChannelOperationFailed, exception, "Channel operation {Operation} failed for channel {ChannelName}", operation, channelName);
    
    #endregion
    
    #region Critical Logging Extensions
    
    // System Critical Events
    public static void LogSystemFailure(this ILogger logger, string component, Exception exception)
        => logger.LogCritical(AvionRelayLogEvents.SystemFailure, exception, "System failure in component {Component}", component);
    
    public static void LogUnhandledException(this ILogger logger, Exception exception)
        => logger.LogCritical(AvionRelayLogEvents.UnhandledException, exception, "Unhandled exception in AvionRelay system");
    
    public static void LogResourceExhausted(this ILogger logger, string resourceType)
        => logger.LogCritical(AvionRelayLogEvents.ResourceExhausted, "Resource exhausted: {ResourceType}", resourceType);
    
    // Transport Critical Events
    public static void LogTransportCriticalFailure(this ILogger logger, string transportType, Exception exception)
        => logger.LogCritical(AvionRelayLogEvents.TransportCriticalFailure, exception, "Critical failure in transport {TransportType}", transportType);
    
    public static void LogPersistentConnectionLost(this ILogger logger, string transportType, string endpoint, Exception exception)
        => logger.LogCritical(AvionRelayLogEvents.PersistentConnectionLost, exception, "Persistent connection lost to {Endpoint} via transport {TransportType}", endpoint, transportType);
    
    public static void LogMessageLoss(this ILogger logger, Guid messageId, string reason)
        => logger.LogCritical(AvionRelayLogEvents.MessageLoss, "Message {MessageId} was lost due to {Reason}", messageId, reason);
    
    // Processing Critical Events
    public static void LogProcessingPipelineBroken(this ILogger logger, string pipelineName, Exception exception)
        => logger.LogCritical(AvionRelayLogEvents.ProcessingPipelineBroken, exception, "Processing pipeline {PipelineName} is broken", pipelineName);
    
    public static void LogStateCorruption(this ILogger logger, Guid messageId, string details)
        => logger.LogCritical(AvionRelayLogEvents.StateCorruption, "State corruption detected for message {MessageId}: {Details}", messageId, details);
    
    #endregion
}
