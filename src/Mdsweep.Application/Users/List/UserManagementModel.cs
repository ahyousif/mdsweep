namespace Mdsweep.Application.Users.List;

public sealed record UserManagementModel(UserModel[] Users, InvitationModel[] Invitations, bool IsAdministrator);
