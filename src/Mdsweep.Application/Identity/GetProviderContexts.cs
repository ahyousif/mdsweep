namespace Mdsweep.Application.Identity;

public sealed record GetProviderContexts(string Subject, Guid? ProviderId = null);
