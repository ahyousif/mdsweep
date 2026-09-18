using Mdsweep.Application.Passengers.Update;

namespace Mdsweep.Api.Features.Passengers.Update;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record UpdatePassengerRequest(
    string? BrokerMemberId,
    string FirstName,
    string LastName,
    DateOnly? DateOfBirth,
    string? PhoneNumber,
    string? AlternatePhoneNumber,
    string? PassengerType,
    string? SpecialNeeds,
    string? Notes
)
{
    public UpdatePassengerCommand ToCommand(Guid id) =>
        new(
            id,
            BrokerMemberId,
            FirstName,
            LastName,
            DateOfBirth is { } dateOfBirth ? new LocalDate(dateOfBirth.Year, dateOfBirth.Month, dateOfBirth.Day) : null,
            PhoneNumber,
            AlternatePhoneNumber,
            PassengerType,
            SpecialNeeds,
            Notes
        );
}
