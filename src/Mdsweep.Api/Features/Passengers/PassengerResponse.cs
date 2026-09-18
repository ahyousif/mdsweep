using Mdsweep.Application.Passengers;

namespace Mdsweep.Api.Features.Passengers;

public sealed record PassengerResponse(
    Guid Id,
    string? BrokerMemberId,
    string FirstName,
    string LastName,
    DateOnly? DateOfBirth,
    string? PhoneNumber,
    string? AlternatePhoneNumber,
    string? PassengerType,
    string? SpecialNeeds,
    string? Notes,
    bool IsActive
)
{
    public static PassengerResponse FromModel(PassengerModel model)
    {
        return new PassengerResponse(
            model.Id,
            model.BrokerMemberId,
            model.FirstName,
            model.LastName,
            model.DateOfBirth?.ToDateOnly(),
            model.PhoneNumber,
            model.AlternatePhoneNumber,
            model.PassengerType,
            model.SpecialNeeds,
            model.Notes,
            model.IsActive
        );
    }
}
