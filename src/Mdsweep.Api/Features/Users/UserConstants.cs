namespace Mdsweep.Api.Features.Users;

public static class UserConstants
{
    public const string Index = "/users";
    public const string IdRoute = Index + "/{id:guid}";
    public const string Tag = "Users";

    public static class Invitations
    {
        public const string Route = Index + "/invitations";
        public const string IdRoute = Route + "/{id:guid}";
        public const string ResendRoute = IdRoute + "/resend";
        public const string Accept = Route + "/accept";
    }
}
