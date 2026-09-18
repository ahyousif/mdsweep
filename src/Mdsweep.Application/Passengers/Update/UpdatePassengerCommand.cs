using Mdsweep.Application.Common.Abstractions;

namespace Mdsweep.Application.Passengers.Update;

public sealed record UpdatePassengerCommand(
    Guid PassengerId,
    string? BrokerMemberId,
    string FirstName,
    string LastName,
    LocalDate? DateOfBirth,
    string? PhoneNumber,
    string? AlternatePhoneNumber,
    string? PassengerType,
    string? SpecialNeeds,
    string? Notes
) : ICommand;
