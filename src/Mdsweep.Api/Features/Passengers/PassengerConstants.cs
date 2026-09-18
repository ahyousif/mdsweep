namespace Mdsweep.Api.Features.Passengers;

public static class PassengerConstants
{
    public const string Route = "/passengers";
    public const string IdRoute = Route + "/{id:guid}";
    public const string DisableRoute = IdRoute + "/disable";
    public const string EnableRoute = IdRoute + "/enable";

    public const string Tag = "Passengers";
}
