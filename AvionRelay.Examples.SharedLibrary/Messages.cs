using AvionRelay.Core.Messages.MessageTypes;

namespace AvionRelay.Examples.SharedLibrary;

public record User(string Name);

/// <summary>
/// Example response for the example.
/// </summary>
public record UserCreated(string Result);

/// <summary>
/// Example command for the example.
/// </summary>
public class CreateUserCommand : Command<UserCreated>
{
    public User User { get; }
    
    public CreateUserCommand(User user)
    {
        User = user;
        Metadata.MessageTypeName = nameof(CreateUserCommand);
    }
}


/// <summary>
/// Example alert for access denied which a child process, or login page might send to some parent process or monitoring system
/// </summary>
/// <param name="User"></param>
/// <param name="Reason"></param>
public class AccessDeniedAlert : Alert
{
    public User User { get; }
    public string Reason { get; }
    
    public AccessDeniedAlert(User user, string reason)
    {
        Metadata.MessageTypeName = nameof(AccessDeniedAlert);
        User = user;
        Reason = reason;
    }
}


/// <summary>
/// Example notification representing a user termination, which might go out to multiple systems users interact with
/// </summary>
public class UserTerminationNotification : Notification
{
    public User User { get; }
    public string Reason { get; }

    public UserTerminationNotification(User user, string reason)
    {
        User = user;
        Reason = reason;
        Metadata.MessageTypeName = nameof(UserTerminationNotification);
    }
}


/// <summary>
/// Example inspection that should return all of the users.
/// </summary>
public class GetAllUsersInspection() : Inspection<List<User>>;
