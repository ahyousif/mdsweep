using Mdsweep.Application.Common.Abstractions;

namespace Mdsweep.Application.Vehicles.List;

public sealed record ListVehiclesQuery : IQuery<IReadOnlyList<VehicleModel>>;
