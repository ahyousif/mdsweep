using Mdsweep.Application.Common.Abstractions;

namespace Mdsweep.Application.Vehicles.Get;

public sealed record GetVehicleQuery(Guid Id) : IQuery<VehicleModel>;
