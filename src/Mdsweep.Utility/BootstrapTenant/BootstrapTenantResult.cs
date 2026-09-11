namespace Mdsweep.Utility.BootstrapTenant;

public enum BootstrapTenantStatus
{
    Created,
    AlreadySatisfied,
    Conflict,
    SchemaUnavailable,
}

public sealed record BootstrapTenantResult(BootstrapTenantStatus Status, string Message)
{
    public bool Succeeded => Status is BootstrapTenantStatus.Created or BootstrapTenantStatus.AlreadySatisfied;
}
