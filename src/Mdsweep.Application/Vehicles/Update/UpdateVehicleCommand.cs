using Mdsweep.Application.Common.Abstractions;

namespace Mdsweep.Application.Vehicles.Update;

public sealed record UpdateVehicleCommand(Guid Id, string DisplayLabel, string Vin) : ICommand;
