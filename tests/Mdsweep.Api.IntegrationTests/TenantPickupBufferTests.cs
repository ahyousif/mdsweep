using Mdsweep.Domain.Tenants;

namespace Mdsweep.Api.IntegrationTests;

public sealed class TenantPickupBufferTests
{
    [Theory]
    [InlineData(-1)]
    [InlineData(121)]
    public void Tenant_rejects_pickup_buffer_values_outside_the_allowed_range(int pickupBufferMinutes)
    {
        var tenant = TenantAggregate.Create("mdsw-eep2-3456", "Synthetic Tenant", "synthetic-tenant");

        Assert.Throws<ArgumentOutOfRangeException>(() => tenant.SetPickupBufferMinutes(pickupBufferMinutes));
    }
}
