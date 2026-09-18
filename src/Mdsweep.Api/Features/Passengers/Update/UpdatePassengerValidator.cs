namespace Mdsweep.Api.Features.Passengers.Update;

public sealed class UpdatePassengerValidator : AbstractValidator<UpdatePassengerRequest>
{
    public UpdatePassengerValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BrokerMemberId).MaximumLength(100);
        RuleFor(x => x.PhoneNumber).MaximumLength(50);
        RuleFor(x => x.AlternatePhoneNumber).MaximumLength(50);
        RuleFor(x => x.PassengerType).MaximumLength(200);
        RuleFor(x => x.SpecialNeeds).MaximumLength(500);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}
