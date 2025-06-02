using System.Collections.Concurrent;
using System.Threading.Channels;
using AvionRelay.Core;
using AvionRelay.Core.Dispatchers;
using AvionRelay.Core.Messages;
using AvionRelay.Core.Messages.MessageTypes;
using AvionRelay.Core.Services;
using HelpfulTypesAndExtensions;
using Metalama.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AvionRelay.Internal;

/// <summary>
/// Broker that manages the communication between internal senders and receivers.
/// Provides a centralized registry for message subscriptions and routing.
/// </summary>
public class AvionRelayInternalMessageBus : AvionRelayMessageBus
{
    private readonly Channel<Package> _messageOutChannel;
    private readonly Channel<IMessageResponse> _responseChannel;
    private readonly ILogger<AvionRelayInternalMessageBus> _logger;
    private readonly IMessageStorage _messageStorage;
    private readonly MessagingManager _messagingManager;
    private readonly AvionRelayOptions _options;

    private bool isChannelReaderActive = false;

    
    private readonly PeriodicTimer _retransmitTimer;
    
    public AvionRelayInternalMessageBus(ILogger<AvionRelayInternalMessageBus> logger, IMessageStorage messageStorage, AvionRelayOptions options, MessagingManager messagingManager)
    {
        _messageOutChannel = Channel.CreateUnboundedPrioritized(new UnboundedPrioritizedChannelOptions<Package>
        {
            Comparer = Comparer<Package>.Create((x, y) => y.Message.Metadata.Priority.CompareTo(x.Message.Metadata.Priority)),
            SingleReader = false,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });
        _responseChannel = Channel.CreateUnbounded<IMessageResponse>();
        _logger = logger;
        _messageStorage = messageStorage;
        _options = options;
        _messagingManager = messagingManager;
        
        //Use a periodic timer to retransmit messages from storage
        _retransmitTimer = new PeriodicTimer(_options.RetryPolicy.RetryInterval);
        Task retransmitTask = Task.Run(async () =>
        {
            _logger.LogInformation("Starting retransmit timer");
            while (await _retransmitTimer.WaitForNextTickAsync())
            {
                if (isChannelReaderActive is false)
                {
                    continue;
                }
                await RetransmitFromStorage();
            }
        });
        retransmitTask.FireAndForget(onError: ex => _logger.LogError(ex, "Error in retransmit timer"));
        
        //A task that reads the next message and passes it to the MessageHandlerRegister
        Task messageReaderTask = Task.Run(async () =>
        {
            _logger.LogInformation("Starting message reader task");
            while (true)
            {
                var package = await ReadNextOutboundMessage();
                if (package != null)
                {
                    //set the state to Received
                    _messagingManager.SetState(package.Message, MessageState.Received);
                    await MessageHandlerRegister.ProcessPackage(package);
                }
                await Task.Delay(10);
            }
        });
        messageReaderTask.FireAndForget(onError: ex => _logger.LogError(ex, "Error in message reader task"));
    }

    /// <inheritdoc />
    public override async Task RegisterMessenger(List<string> supportedMessageNames)
    {
    }

    /// <inheritdoc />
    public override async Task<MessageResponse<TResponse?>> ExecuteCommand<TCommand, TResponse>(TCommand command, CancellationToken? cancellationToken = null, TimeSpan? timeout = null) where TResponse : default
    {
        TimeSpan _timeout = timeout ?? _options.MessageTimeout;
        return (await SendAndWaitForResponseAsync<TCommand, TResponse>(Package.Create(command), _timeout)).FirstOrDefault();
    }

    /// <inheritdoc />
    public override async Task<List<MessageResponse<TResponse?>>> RequestInspection<TInspection, TResponse>(TInspection inspection, CancellationToken? cancellationToken = null, TimeSpan? timeout = null) where TResponse : default
    {
        TimeSpan _timeout = timeout ?? _options.MessageTimeout;
        var package = Package.Create(inspection);
        return await SendAndWaitForResponseAsync<TInspection, TResponse>(package, _timeout);
    }

    /// <inheritdoc />
    public override async Task PublishNotification<TNotification>(TNotification notification, CancellationToken? cancellationToken = null, TimeSpan? timeout = null)
    {
        TimeSpan _timeout = timeout ?? _options.MessageTimeout;
        await PublishAsync<TNotification>(Package.Create(notification), _timeout);
    }

    /// <inheritdoc />
    public override async Task SendAlert<TAlert>(TAlert alert, CancellationToken? cancellationToken = null, TimeSpan? timeout = null)
    {
        TimeSpan _timeout = timeout ?? _options.MessageTimeout;
        await PublishAsync<TAlert>(Package.Create(alert), _timeout);
    }
    
    /// <summary>
    /// Responds to a message.
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <typeparam name="TResponse">The response type</typeparam>
    /// <param name="messageId">The ID of the message being responded to</param>
    /// <param name="response">The response</param>
    public override async Task RespondToMessage<T, TResponse>(Guid messageId, TResponse response, MessageReceiver responder)
    {
        var package = _messageStorage.RetrievePackage(messageId);
        ArgumentNullException.ThrowIfNull(package);
        _messagingManager.SetState(package.Message, MessageState.Responded);
        await _responseChannel.Writer.WriteAsync(new MessageResponse<TResponse> { Response = response, MessageId = messageId, Acknowledger = responder });
    }
    
    /// <summary>
    /// Acknowledges a message.
    /// </summary>
    /// <param name="messageId"></param>
    /// <param name="acknowledger"></param>
    /// <typeparam name="T"></typeparam>
    public override async Task AcknowledgeMessage<T>(Guid messageId, MessageReceiver acknowledger)
    {
        await _responseChannel.Writer.WriteAsync(new MessageAcknowledgement { MessageId = messageId, Acknowledger  = acknowledger});
    }

    /// <inheritdoc />
    public override async Task<Package?> ReadNextOutboundMessage(CancellationToken cancellationToken = default)
    {
        //wait to read a new message or return null if canceled
        isChannelReaderActive = true;
        if (!await _messageOutChannel.Reader.WaitToReadAsync(cancellationToken))
        {
            isChannelReaderActive = false;
            return null;
        }

        Package package = await _messageOutChannel.Reader.ReadAsync(cancellationToken);
        {
            _logger.LogDebug("Read message {MessageId} from channel", package.Message.Metadata.MessageId);
            return package;
        }
    }


    /// <summary>
    /// Publishes a message to all subscribers.
    /// </summary>
    /// <typeparam name="TMessage">The message type</typeparam>
    /// <param name="package">The message envelope</param>
    /// <param name="timeout">The maximum time to wait for a response</param>
    /// <returns>A task that completes when the message is published</returns>
    internal async Task PublishAsync<TMessage>(Package package, TimeSpan? timeout = null) where TMessage : AvionRelayMessage
    {
        try
        {
            var messageType = typeof(TMessage);
            _logger.LogInformation("Publishing message {MessageId} of type {MessageType}", package.Message.Metadata.MessageId, messageType.Name);
            // Send the message to the channel
            await SendViaChannel(package);
        
            using var cts = new CancellationTokenSource(timeout ?? _options.MessageTimeout);
        
            //get the number of receivers
            int receiverCount = MessageHandlerRegister.GetReceiverCount(messageType);
            //get number of receivers that have already acknowledged
            int acknowledgedCount = package.Message.Metadata.Acknowledgements.Count;
        
            while (await _responseChannel.Reader.WaitToReadAsync(cts.Token))
            {
                if (_responseChannel.Reader.TryRead(out var response))
                {
                    if (response.MessageId == package.Message.Metadata.MessageId)
                    {
                        _logger.LogDebug("Read acknowledgement for message {MessageId} from receiver {ReceiverId}", package.Message.Metadata.MessageId, response.Acknowledger.ReceiverId);
                        Acknowledgement ack = new(response.MessageId, response.Acknowledger);
                        _messagingManager.AcknowledgeMessage(package.Message,ack);
                        acknowledgedCount = package.Message.Metadata.Acknowledgements.Count;
                    }
                }
                if (acknowledgedCount >= receiverCount)
                {
                    _logger.LogDebug("All receivers have acknowledged message {MessageId}", package.Message.Metadata.MessageId);
                    break;
                }
            }

        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Timeout waiting for acknowledgement of message {MessageId}", package.Message.Metadata.MessageId);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error publishing message {MessageId}", package.Message.Metadata.MessageId);
        }
    }
    
    
    /// <summary>
    /// Sends a message and waits for a response.
    /// </summary>
    /// <typeparam name="TMessage">The message type</typeparam>
    /// <typeparam name="TResponse">The response type</typeparam>
    /// <param name="package">The message envelope</param>
    /// <param name="timeout">The maximum time to wait for a response</param>
    /// <returns>The response, or null if the timeout expired</returns>
    internal async Task<List<MessageResponse<TResponse?>>> SendAndWaitForResponseAsync<TMessage, TResponse>(Package package, TimeSpan timeout) where TMessage : AvionRelayMessage, IRespond<TResponse>
    {
        var messageId = package.Message.Metadata.MessageId;
        
        //if task is an Inspection, result will be a List<TResponse>
        var responseList = new List<MessageResponse<TResponse?>>();
        try
        {
            // Publish the message
            await SendViaChannel(package);
            
            // create a cancellation token that stops after the timeout
            using var cts = new CancellationTokenSource(timeout);
            
            var messageType = typeof(TMessage);
            //get the number of receivers
            int receiverCount = MessageHandlerRegister.GetReceiverCount(messageType);
            //get number of receivers that have already acknowledged
            int responseCount = package.Message.Metadata.Acknowledgements.Count;
            
            // Wait for the response
            Console.WriteLine("Waiting for response");
            
           
            
            while (await _responseChannel.Reader.WaitToReadAsync(cts.Token))
            {
                if (_responseChannel.Reader.TryRead(out var response))
                {
                    _logger.LogDebug("Read response for message {MessageId} from channel", messageId);
                    if (response.MessageId == messageId)
                    {
                        Acknowledgement ack = new(messageId, response.Acknowledger);
                        package.Message.Metadata.Acknowledgements.Add(ack);
                        _messagingManager.SetState(package.Message, MessageState.ResponseReceived);
                        responseCount = package.Message.Metadata.Acknowledgements.Count;
                        responseList.Add((MessageResponse<TResponse?>)response);
                    }
                }
                if (responseCount >= receiverCount)
                {
                    _logger.LogDebug("All receivers have acknowledged message {MessageId}", messageId);
                    return responseList;
                }
            }
            _logger.LogWarning("Timeout waiting for response to message {MessageId}", messageId);
            return responseList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error waiting for response to message {MessageId}", messageId);
            return responseList;
        }
    }
   

    /// <summary>
    /// Sends a message via the channel.
    /// </summary>
    /// <param name="package">The message envelope</param>
    /// <returns>A task that completes when the message is written to the channel</returns>
    internal async Task SendViaChannel(Package package)
    {
        if (package.Message.Metadata.State is not MessageState.Created)
        {
            throw new Exception("Message must be in Created state to be sent");
        }
        if (isChannelReaderActive is false)
        {
            //store so we can retransmit later when the channel is active
            _logger.LogWarning("No readers available, storing message {MessageId} for later processing", package.Message.Metadata.MessageId);
            _messageStorage.StorePackage(package,true);
        }
        else
        {
            _messagingManager.SetState(package.Message, MessageState.Sent);
            //Store so we can get later to update state on response
            _messageStorage.StorePackage(package,false);
            _logger.LogDebug("Writing message {MessageId} to channel", package.Message.Metadata.MessageId);
            await _messageOutChannel.Writer.WriteAsync(package);
        }
    }
    
    /// <summary>
    /// Retransmits messages from storage to the channel.
    /// Messages might be stored for a number of reasons, including:
    /// - No readers available when the message was originally sent
    /// - Message processing taking longer than expected
    /// - Network or system issues preventing message delivery
    /// </summary>
    internal async Task RetransmitFromStorage()
    {
        var package = _messageStorage.RetrieveNextPackage();
        if (package == null)
        {
            return;
        }
        await SendViaChannel(package);
    }
}
