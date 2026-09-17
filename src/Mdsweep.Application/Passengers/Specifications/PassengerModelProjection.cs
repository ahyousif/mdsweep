using Mdsweep.Domain.Passengers;

namespace Mdsweep.Application.Passengers.Specifications;

internal sealed class PassengerModelProjection : Specification<PassengerAggregate, PassengerModel>
{
    public static PassengerModelProjection Instance { get; } = new();

    private PassengerModelProjection()
    {
        Query.Select(passenger => new PassengerModel
        {
            Id = passenger.Id,
            BrokerMemberId = passenger.BrokerMemberId,
            FirstName = passenger.FirstName,
            LastName = passenger.LastName,
            DateOfBirth = passenger.DateOfBirth,
            PhoneNumber = passenger.PhoneNumber,
            AlternatePhoneNumber = passenger.AlternatePhoneNumber,
            PassengerType = passenger.PassengerType,
            SpecialNeeds = passenger.SpecialNeeds,
            Notes = passenger.Notes,
        });
    }
}
