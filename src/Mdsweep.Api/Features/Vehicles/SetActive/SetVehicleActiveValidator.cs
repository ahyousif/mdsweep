namespace Mdsweep.Api.Features.Vehicles.SetActive;

public sealed class SetVehicleActiveValidator : AbstractValidator<SetVehicleActiveRequest>
{
    public SetVehicleActiveValidator() => RuleFor(x => x.IsActive).NotNull().OverridePropertyName("isActive");
}
