using AvionRelay.Core.Messages.MessageTypes;

namespace SharedLibrary;

public record User(string Name);

/// <summary>
/// Example response for the example.
/// </summary>
public record UserCreated(string Result);

/// <summary>
/// Example command for the example.
/// </summary>
public record CreateUserCommand(User User) : Command<UserCreated>;


/// <summary>
/// Example alert for access denied which a child process, or login page might send to some parent process or monitoring system
/// </summary>
/// <param name="User"></param>
/// <param name="Reason"></param>
public record AccessDeniedAlert(User User, string Reason) : Alert;


/// <summary>
/// Example notification representing a user termination, which might go out to multiple systems users interact with
/// </summary>
public record UserTerminationNotification(User User, string Reason) : Notification;


/// <summary>
/// Example inspection that should return all of the users.
/// </summary>
public record GetAllUsersInspection() : Inspection<List<User>>;
