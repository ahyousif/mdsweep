using Mdsweep.Api.Configuration;
using Mdsweep.Application.Trips.Scheduling;
using Mdsweep.Application.Trips.SetScheduledPickupTime;

namespace Mdsweep.Api.IntegrationTests;

public sealed class ScheduleTripRetryPolicyTests
{
    [Fact]
    public void Retry_policy_applies_only_to_schedule_trip_commands()
    {
        Assert.True(ScheduleTripRetryPolicy.AppliesTo(typeof(ScheduleTripCommand)));
        Assert.False(ScheduleTripRetryPolicy.AppliesTo(typeof(SetScheduledPickupTimeCommand)));
    }
}
