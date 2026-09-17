using Mdsweep.Domain.Vehicles;

namespace Mdsweep.Api.Features.Vehicles.Create;

public sealed class CreateVehicleValidator : AbstractValidator<CreateVehicleRequest>
{
    public CreateVehicleValidator()
    {
        RuleFor(x => x.DisplayLabel)
            .NotEmpty()
            .MaximumLength(VehicleAggregate.MaxDisplayLabelLength)
            .OverridePropertyName("displayLabel");
        RuleFor(x => x.Vin)
            .NotEmpty()
            .Length(17)
            .Matches("^[A-HJ-NPR-Za-hj-npr-z0-9]{17}$")
            .WithMessage("VIN must contain 17 letters or digits, excluding I, O and Q.")
            .OverridePropertyName("vin");
    }
}
