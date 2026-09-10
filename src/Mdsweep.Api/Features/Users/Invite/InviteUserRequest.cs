namespace Mdsweep.Api.Features.Users.Invite;

public sealed record InviteUserRequest(string Email, string FirstName, string LastName, string[] Roles);
