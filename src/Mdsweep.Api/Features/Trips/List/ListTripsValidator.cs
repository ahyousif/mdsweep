namespace Mdsweep.Api.Features.Trips.List;

public sealed class ListTripsRequestValidator : AbstractValidator<ListTripsRequest>
{
    public ListTripsRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);

        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);

        RuleFor(x => x.SortBy).IsInEnum();

        RuleFor(x => x.SortDirection).IsInEnum();

        RuleFor(x => x.ServiceDate)
            .Must(BeValidServiceDate)
            .When(x => x.ServiceDate is not null)
            .WithMessage("Service date must use ISO-8601 format (yyyy-MM-dd).");
    }

    private static bool BeValidServiceDate(string? value) => value is null || LocalDatePattern.Iso.Parse(value).Success;
}
