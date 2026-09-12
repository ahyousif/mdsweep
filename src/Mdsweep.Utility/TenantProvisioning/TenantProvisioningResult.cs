namespace Mdsweep.Utility.TenantProvisioning;

public enum TenantProvisioningStatus
{
    Provisioned,
    AlreadySatisfied,
    Conflict,
}

public sealed record TenantProvisioningResult(TenantProvisioningStatus Status, string Message)
{
    public bool Succeeded =>
        Status is TenantProvisioningStatus.Provisioned or TenantProvisioningStatus.AlreadySatisfied;
}
