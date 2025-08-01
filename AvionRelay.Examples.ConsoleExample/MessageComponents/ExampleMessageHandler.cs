﻿using AvionRelay.Core.Dispatchers;
using Microsoft.Extensions.Logging;
using AvionRelay.Examples.SharedLibrary;

namespace AvionRelay.Examples.ConsoleExample;

public class ExampleMessageHandler
{

    private readonly AvionRelayMessageBus _bus;
    private readonly ILogger<ExampleMessageHandler> _logger;

    public static readonly Guid HandlerID = Guid.CreateVersion7();
    
    public ExampleMessageHandler(AvionRelayMessageBus bus, ILogger<ExampleMessageHandler> logger)
    {
        _bus = bus;
        _logger = logger;
    }
    

    public async Task<UserCreated> HandleCreateUserCommand(CreateUserCommand command)
    {
        //stuff and things
        _logger.LogInformation("Handling create user command");
        var user = new UserCreated(command.User.Name);
        var responder = new MessageReceiver(HandlerID.ToString(), nameof(ExampleMessageHandler));
        await _bus.RespondToMessage<CreateUserCommand,UserCreated>(command.Metadata.MessageId, user, responder);
        return user;
    }
    
    //handler for a Access Denied Alert
    public async Task HandleAccessDeniedAlert(AccessDeniedAlert alert)
    {
        //stuff and things
        _logger.LogInformation("Handling access denied alert");
        var responder = new MessageReceiver(HandlerID.ToString(), nameof(ExampleMessageHandler));
        await _bus.AcknowledgeMessage<AccessDeniedAlert>(alert.Metadata.MessageId, responder);
    }
    
    //handler for a get all users inspection
    public async Task<List<User>> HandleGetAllUsersInspection(GetAllUsersInspection inspection)
    {
        //stuff and things
        _logger.LogInformation("Handling get all users inspection");
        var users = new List<User> { new User("John"), new User("Jane") };
        var responder = new MessageReceiver(HandlerID.ToString(), nameof(ExampleMessageHandler));
        await _bus.RespondToMessage<GetAllUsersInspection,List<User>>(inspection.Metadata.MessageId, users, responder);
        return users;
    }
    
    // handler for a user terminated notification
    public async Task HandleUserTerminatedNotification(UserTerminationNotification notification)
    {
        //stuff and things
        _logger.LogInformation("Handling user terminated notification from first handler");
        _logger.LogInformation("User {UserName} was terminated for reason: {Reason}", notification.User.Name, notification.Reason);
    }
}

public class SecondExampleMessageHandler
{
    private readonly AvionRelayMessageBus _bus;
    private readonly ILogger<SecondExampleMessageHandler> _logger;

    public static readonly Guid HandlerID = Guid.CreateVersion7();

    public SecondExampleMessageHandler(AvionRelayMessageBus bus, ILogger<SecondExampleMessageHandler> logger)
    {
        _bus = bus;
        _logger = logger;
    }

    //handler for a get all users inspection
    public async Task<List<User>> HandleGetAllUsersInspection(GetAllUsersInspection inspection)
    {
        //stuff and things
        _logger.LogInformation("Handling get all users inspection");
        var users = new List<User>
        {
            new User("Jason"),
            new User("Jenny")
        };
        var responder = new MessageReceiver(HandlerID.ToString(), nameof(SecondExampleMessageHandler));

        await _bus.RespondToMessage<GetAllUsersInspection, List<User>>(inspection.Metadata.MessageId, users, responder);
        return users;
    }

    // handler for a user terminated notification
    public async Task HandleUserTerminatedNotification(UserTerminationNotification notification)
    {
        //stuff and things
        _logger.LogInformation("Handling user terminated notification from second handler");
        _logger.LogInformation("User {UserName} was terminated for reason: {Reason}", notification.User.Name, notification.Reason);
    }
}