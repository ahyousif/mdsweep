using Mdsweep.Application.Common.Abstractions;

namespace Mdsweep.Application.Vehicles.Create;

public sealed record CreateVehicleCommand(string DisplayLabel, string Vin) : ICommand<Guid>;
