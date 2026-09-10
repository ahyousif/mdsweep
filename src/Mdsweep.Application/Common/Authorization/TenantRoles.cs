namespace Mdsweep.Application.Common.Authorization;

public static class TenantRoles
{
    public const string Administrator = "Administrator";
    public const string Dispatcher = "Dispatcher";
    public const string Driver = "Driver";

    public static readonly string[] All = [Administrator, Dispatcher, Driver];
}
