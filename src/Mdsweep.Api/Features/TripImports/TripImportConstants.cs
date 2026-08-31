namespace Mdsweep.Api.Features.TripImports;

public static class TripImportConstants
{
    public const string Route = "/api/trip-imports";
    public const string IdRoute = Route + "/{id:guid}";
    public const string ApplyRoute = IdRoute + "/apply";
    public const string Tag = "Trip Imports";
}
