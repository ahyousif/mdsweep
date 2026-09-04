namespace Mdsweep.Api.Features.Trips.Import;

public sealed class ImportTripsRequestValidator : AbstractValidator<ImportTripsRequest>
{
    public ImportTripsRequestValidator()
    {
        RuleFor(request => request.File).NotNull().WithMessage("Choose a CSV or XLSX trip file.");
        When(request => request.File is not null, () =>
        {
            RuleFor(request => request.File!.Length).GreaterThan(0).WithMessage("The trip file cannot be empty.");
            RuleFor(request => request.File!.FileName)
                .Must(name => name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) || name.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                .WithMessage("Only CSV and XLSX trip files are supported.");
        });
    }
}
