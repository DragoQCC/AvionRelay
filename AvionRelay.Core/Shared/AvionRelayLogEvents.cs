namespace AvionRelay.Core;

/// <summary>
/// /// Event IDs:
///     0-999 is for information,
///     1000-1999 is for debug,
///     2000-2999 is for trace
///     3000-3999 is for warning,
///     4000-4999 is for error,
///     5000-5999 is for critical
/// </summary>
public static class AvionRelayLogEvents
{
    #region Information Events (0-999)
    
    // Message Lifecycle Information Events
    public const int MessageCreated = 100;
    public const int MessageSent = 101;
    public const int MessageReceived = 102;
    public const int MessageAcknowledged = 103;
    public const int MessageResponded = 104;
    public const int MessageResponseReceived = 105;
    public const int MessageCompleted = 106;
    
    // System Information Events
    public const int SystemStarted = 200;
    public const int SystemStopped = 201;
    public const int ProcessorRegistered = 202;
    public const int HandlerRegistered = 203;
    public const int TransportConnected = 204;
    public const int TransportDisconnected = 205;
    
    // Channel Information Events
    public const int ChannelCreated = 300;
    public const int MessageEnqueued = 301;
    public const int MessageDequeued = 302;
    public const int SubscriptionAdded = 303;
    public const int SubscriptionRemoved = 304;
    
    #endregion
    
    #region Debug Events (1000-1999)
    
    // Message Processing Debug Events
    public const int MessageProcessingStarted = 1000;
    public const int MessageProcessingCompleted = 1001;
    public const int StateTransition = 1002;
    public const int ProcessorChainExecutionStarted = 1003;
    public const int ProcessorChainExecutionCompleted = 1004;
    public const int ProcessorExecuted = 1005;
    
    // Channel Debug Events
    public const int ChannelStatistics = 1100;
    public const int PriorityQueueStatistics = 1101;
    public const int MessagePriorityChanged = 1102;
    
    // System Debug Events
    public const int ConfigurationLoaded = 1200;
    public const int DependencyInjectionSetup = 1201;
    
    #endregion
    
    #region Trace Events (2000-2999)
    
    // Detailed Message Tracing
    public const int MessageEnvelopeCreated = 2000;
    public const int MessageContextCreated = 2001;
    public const int MessageIdGenerated = 2002;
    public const int MessagePropertyAccessed = 2003;
    public const int MessagePropertyChanged = 2004;
    
    // Detailed Processing Tracing
    public const int ProcessorMethodEntered = 2100;
    public const int ProcessorMethodExited = 2101;
    public const int StateMethodEntered = 2102;
    public const int StateMethodExited = 2103;
    
    // Channel Tracing
    public const int ChannelOperationStarted = 2200;
    public const int ChannelOperationCompleted = 2201;
    
    #endregion
    
    #region Warning Events (3000-3999)
    
    // Message Processing Warnings
    public const int MessageProcessingDelayed = 3000;
    public const int MessageRetryAttempted = 3001;
    public const int MessagePriorityDegraded = 3002;
    public const int MessageTimeoutWarning = 3003;
    public const int MessageSizeExceedsRecommended = 3004;
    public const int ProcessorSkipped = 3005;
    public const int UnexpectedStateTransition = 3006;
    
    // System Warnings
    public const int ChannelBackpressure = 3100;
    public const int ResourceUtilizationHigh = 3101;
    public const int ConfigurationSuboptimal = 3102;
    public const int TransportConnectionUnstable = 3103;
    public const int HandlerPerformanceDegraded = 3104;
    
    #endregion
    
    #region Error Events (4000-4999)
    
    // Message Processing Errors
    public const int MessageProcessingFailed = 4000;
    public const int MessageValidationFailed = 4001;
    public const int MessageDeserializationFailed = 4002;
    public const int MessageSerializationFailed = 4003;
    public const int MessageHandlingFailed = 4004;
    public const int InvalidMessageState = 4005;
    public const int MessageRetryLimitExceeded = 4006;
    public const int MessageTimeoutExceeded = 4007;
    
    // Transport Errors
    public const int TransportSendFailed = 4100;
    public const int TransportReceiveFailed = 4101;
    public const int TransportConnectionFailed = 4102;
    public const int TransportAuthenticationFailed = 4103;
    
    // System Errors
    public const int ProcessorExecutionFailed = 4200;
    public const int ChannelOperationFailed = 4201;
    public const int HandlerExecutionFailed = 4202;
    public const int DependencyResolutionFailed = 4203;
    public const int ConfigurationError = 4204;
    
    #endregion
    
    #region Critical Events (5000-5999)
    
    // System Critical Events
    public const int SystemFailure = 5000;
    public const int UnhandledException = 5001;
    public const int ResourceExhausted = 5002;
    public const int DataCorruption = 5003;
    public const int SecurityBreach = 5004;
    
    // Transport Critical Events
    public const int TransportCriticalFailure = 5100;
    public const int PersistentConnectionLost = 5101;
    public const int MessageLoss = 5102;
    
    // Processing Critical Events
    public const int ProcessingPipelineBroken = 5200;
    public const int StateCorruption = 5201;
    public const int DeadlockDetected = 5202;
    public const int CriticalDependencyFailure = 5203;
    
    #endregion
}