using Mdsweep.Application.DriverWork;

namespace Mdsweep.Infrastructure.DriverWork;

public sealed class SystemDriverWorkClock : IDriverWorkClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
