using Mdsweep.Domain.Common.Abstractions;
using Mdsweep.Domain.Common.Extensions;
using Mdsweep.Domain.Passengers.Events;

namespace Mdsweep.Domain.Passengers;

public sealed class PassengerAggregate : AggregateRoot<Guid>, ITenanted
{
    private PassengerAggregate()
        : base(default) { }

    private PassengerAggregate(
        Guid id,
        string? brokerMemberId,
        string firstName,
        string lastName,
        LocalDate? dateOfBirth,
        string? phoneNumber,
        string? alternatePhoneNumber,
        string? passengerType,
        string? specialNeeds
    )
        : base(id)
    {
        BrokerMemberId = brokerMemberId;
        FirstName = firstName;
        LastName = lastName;
        DateOfBirth = dateOfBirth;
        PhoneNumber = phoneNumber;
        AlternatePhoneNumber = alternatePhoneNumber;
        PassengerType = passengerType;
        SpecialNeeds = specialNeeds;
    }

    public string? TenantId { get; set; }

    public string? BrokerMemberId { get; private set; }
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public LocalDate? DateOfBirth { get; private set; }
    public string? PhoneNumber { get; private set; }
    public string? AlternatePhoneNumber { get; private set; }
    public string? PassengerType { get; private set; }
    public string? SpecialNeeds { get; private set; }
    public string? Notes { get; private set; }

    public static PassengerAggregate Create(
        string? brokerMemberId,
        string firstName,
        string lastName,
        LocalDate? dateOfBirth = null,
        string? phoneNumber = null,
        string? alternatePhoneNumber = null,
        string? passengerType = null,
        string? specialNeeds = null
    )
    {
        Guard.Against.Invalid(
            brokerMemberId is not null && string.IsNullOrWhiteSpace(brokerMemberId),
            "Broker member ID cannot be blank when supplied."
        );

        var passenger = new PassengerAggregate(
            Guid.CreateVersion7(),
            brokerMemberId?.ToUpperInvariant(),
            Guard.Against.NullOrWhiteSpace(firstName, nameof(firstName)),
            Guard.Against.NullOrWhiteSpace(lastName, nameof(lastName)),
            dateOfBirth,
            phoneNumber,
            alternatePhoneNumber,
            passengerType,
            specialNeeds
        );

        passenger.AddDomainEvent(new PassengerCreatedDomainEvent(passenger.Id));

        return passenger;
    }

    public void UpdateMtmDetails(
        string firstName,
        string lastName,
        LocalDate? dateOfBirth,
        string? phoneNumber,
        string? alternatePhoneNumber,
        string? passengerType,
        string? specialNeeds
    )
    {
        FirstName = Guard.Against.NullOrWhiteSpace(firstName, nameof(firstName));
        LastName = Guard.Against.NullOrWhiteSpace(lastName, nameof(lastName));
        DateOfBirth = dateOfBirth;
        PhoneNumber = phoneNumber;
        AlternatePhoneNumber = alternatePhoneNumber;
        PassengerType = passengerType;
        SpecialNeeds = specialNeeds;
    }

    public void UpdateNotes(string? notes) => Notes = notes;
}
