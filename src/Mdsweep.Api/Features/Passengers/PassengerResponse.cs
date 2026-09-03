using Mdsweep.Application.Passengers;

namespace Mdsweep.Api.Features.Passengers;

public sealed record PassengerResponse(Guid Id, string? BrokerMemberId, string FirstName, string LastName)
{
    public static PassengerResponse FromModel(PassengerModel model)
    {
        return new PassengerResponse(
            model.Id,
            model.BrokerMemberId,
            model.FirstName,
            model.LastName
        );
    }
}
