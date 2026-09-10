namespace Mdsweep.Api.Features.Users.Update;

public sealed record UpdateUserRequest(string DisplayName, string[] Roles, bool IsActive, int Version);
