namespace Mdsweep.Api.Features.Vehicles.Update;

public sealed class UpdateVehicleValidator : AbstractValidator<UpdateVehicleRequest>
{
    public UpdateVehicleValidator(IClock clock)
    {
        RuleFor(x => x.DisplayLabel).NotEmpty().MaximumLength(100).OverridePropertyName("displayLabel");
        RuleFor(x => x.Year)
            .InclusiveBetween(1900, clock.GetCurrentInstant().InUtc().Year + 1)
            .OverridePropertyName("year");
        RuleFor(x => x.Make).MaximumLength(100).OverridePropertyName("make");
        RuleFor(x => x.Model).MaximumLength(100).OverridePropertyName("model");
        RuleFor(x => x.Vin)
            .NotEmpty()
            .Length(17)
            .Matches("^[A-HJ-NPR-Za-hj-npr-z0-9]{17}$")
            .WithMessage("VIN must contain 17 letters or digits, excluding I, O and Q.")
            .OverridePropertyName("vin");
    }
}
