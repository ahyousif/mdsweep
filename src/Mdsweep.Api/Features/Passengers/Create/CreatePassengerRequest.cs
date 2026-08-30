using Mdsweep.Application.Passengers.Create;

namespace Mdsweep.Api.Features.Passengers.Create;

public sealed record CreatePassengerRequest(
    string FirstName,
    string LastName,
    LocalDate DateOfBirth,
    string Gender
)
{
    public CreatePassengerCommand ToCommand() => new(FirstName, LastName, DateOfBirth, Gender);
}
