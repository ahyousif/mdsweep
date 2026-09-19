using Mdsweep.Application.Vehicles;

namespace Mdsweep.Api.Features.Vehicles;

public sealed record VehicleResponse(
    Guid Id,
    string DisplayLabel,
    string Vin,
    bool IsActive,
    int? Year = null,
    string? Make = null,
    string? Model = null
)
{
    public static VehicleResponse FromModel(VehicleModel model) =>
        new(model.Id, model.DisplayLabel, model.Vin, model.IsActive, model.Year, model.Make, model.Model);
}
