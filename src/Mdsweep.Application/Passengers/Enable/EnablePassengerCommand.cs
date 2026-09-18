using Mdsweep.Application.Common.Abstractions;

namespace Mdsweep.Application.Passengers.Enable;

public sealed record EnablePassengerCommand(Guid PassengerId) : ICommand;
