using Mdsweep.Domain.Common.Abstractions;

namespace Mdsweep.Domain.Trips;

public sealed class JourneyAggregate : AggregateRoot<Guid>, ITenanted
{
    private JourneyAggregate()
        : base(default) { }

    private JourneyAggregate(Guid id)
        : base(id) { }

    public string? TenantId { get; set; }

    public static JourneyAggregate Create() => new(Guid.CreateVersion7());
}
