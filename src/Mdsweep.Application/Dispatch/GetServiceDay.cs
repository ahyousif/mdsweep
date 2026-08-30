namespace Mdsweep.Application.Dispatch;

public sealed record GetServiceDay(Guid ProviderId, DateOnly ServiceDate);
