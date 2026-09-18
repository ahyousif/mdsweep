using Mdsweep.Application.Common.Abstractions;

namespace Mdsweep.Application.Passengers.Disable;

public sealed record DisablePassengerCommand(Guid PassengerId) : ICommand;
