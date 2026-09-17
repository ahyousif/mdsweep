using Mdsweep.Application.Common.Abstractions;

namespace Mdsweep.Application.Vehicles.SetActive;

public sealed record SetVehicleActiveCommand(Guid Id, bool IsActive) : ICommand;
