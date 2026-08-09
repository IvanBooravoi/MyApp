namespace MyApp.Application.DTO;

public sealed record UserListItem(
    Guid Id,
    string FirstName,
    string MiddleName,
    string LastName,
    string UserName,
    Guid PositionId,
    string Position,
    string Role,
    DateTime CreatedAt);

public sealed record CreateUserResponse(Guid Id, string TemporaryPassword);

public sealed record UpdateUserResponse(Guid Id);
