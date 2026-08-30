namespace Mdsweep.Application.Identity;

public sealed record ProviderContext(Guid ProviderId, Guid AppUserId, string Role);
