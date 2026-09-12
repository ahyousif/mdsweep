namespace Mdsweep.Application.Users;

public sealed record UserModel(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string DisplayName,
    string[] Roles,
    bool IsActive,
    int Version
);
