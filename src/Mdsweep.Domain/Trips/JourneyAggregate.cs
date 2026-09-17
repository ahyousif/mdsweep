using Mdsweep.Domain.Common.Abstractions;

namespace Mdsweep.Domain.Trips;

public sealed class JourneyAggregate : AggregateRoot<Guid>, ITenanted
{
    private JourneyAggregate()
        : base(default) { }

    private JourneyAggregate(Guid id, JourneyGroupingType groupingType)
        : base(id)
    {
        GroupingType = groupingType;
    }

    public string? TenantId { get; set; }
    public JourneyGroupingType GroupingType { get; private set; }

    public static JourneyAggregate Create(JourneyGroupingType groupingType) => new(Guid.CreateVersion7(), groupingType);

    public void MarkManual() => GroupingType = JourneyGroupingType.Manual;
}
