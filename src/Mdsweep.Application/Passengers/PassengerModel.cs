using Mdsweep.Domain.Passengers;

namespace Mdsweep.Application.Passengers;

public sealed record PassengerModel
{
    public Guid Id { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public string? BrokerMemberId { get; init; }
    public LocalDate? DateOfBirth { get; init; }
    public string? PhoneNumber { get; init; }
    public string? AlternatePhoneNumber { get; init; }
    public string? PassengerType { get; init; }
    public string? SpecialNeeds { get; init; }
    public string? Notes { get; init; }
    public bool IsActive { get; init; }

    public static PassengerModel FromAggregate(PassengerAggregate passenger) =>
        new()
        {
            Id = passenger.Id,
            FirstName = passenger.FirstName,
            LastName = passenger.LastName,
            BrokerMemberId = passenger.BrokerMemberId,
            DateOfBirth = passenger.DateOfBirth,
            PhoneNumber = passenger.PhoneNumber,
            AlternatePhoneNumber = passenger.AlternatePhoneNumber,
            PassengerType = passenger.PassengerType,
            SpecialNeeds = passenger.SpecialNeeds,
            Notes = passenger.Notes,
            IsActive = passenger.IsActive,
        };
}
