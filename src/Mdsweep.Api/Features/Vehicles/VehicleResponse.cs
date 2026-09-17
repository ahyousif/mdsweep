using Mdsweep.Application.Vehicles;

namespace Mdsweep.Api.Features.Vehicles;

public sealed record VehicleResponse(Guid Id, string DisplayLabel, string Vin, bool IsActive)
{
    public static VehicleResponse FromModel(VehicleModel model) =>
        new(model.Id, model.DisplayLabel, model.Vin, model.IsActive);
}
