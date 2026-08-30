namespace Mdsweep.Application.Dispatch;

public sealed record VehicleResponse(Guid Id, string DisplayName, string Vin, bool IsActive);
