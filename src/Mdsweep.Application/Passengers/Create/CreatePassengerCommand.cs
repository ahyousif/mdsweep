using Mdsweep.Application.Common.Abstractions;

namespace Mdsweep.Application.Passengers.Create;

public record CreatePassengerCommand(
    string FirstName,
    string LastName,
    LocalDate DateOfBirth,
    string Gender
) : IRequest<Guid>;
