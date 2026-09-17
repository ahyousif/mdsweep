using Mdsweep.Domain.Passengers;

namespace Mdsweep.Application.Passengers.List;

internal sealed class PassengerListProjection : Specification<PassengerAggregate, PassengerModel>
{
    public static PassengerListProjection Instance { get; } = new();

    private PassengerListProjection()
    {
        Query.Select(passenger => new PassengerModel
        {
            Id = passenger.Id,
            BrokerMemberId = passenger.BrokerMemberId,
            FirstName = passenger.FirstName,
            LastName = passenger.LastName,
            PhoneNumber = passenger.PhoneNumber,
            PassengerType = passenger.PassengerType,
        });
    }
}
