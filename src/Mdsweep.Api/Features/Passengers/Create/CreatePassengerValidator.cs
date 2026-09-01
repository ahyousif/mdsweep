namespace Mdsweep.Api.Features.Passengers.Create;

public sealed class CreatePassengerValidator : AbstractValidator<CreatePassengerRequest>
{
    public CreatePassengerValidator()
    {
        RuleFor(request => request.FirstName)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("First name is required.")
            .MaximumLength(200)
            .WithMessage("First name must be 200 characters or fewer.")
            .OverridePropertyName("firstName");

        RuleFor(request => request.LastName)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Last name is required.")
            .MaximumLength(200)
            .WithMessage("Last name must be 200 characters or fewer.")
            .OverridePropertyName("lastName");
    }
}
