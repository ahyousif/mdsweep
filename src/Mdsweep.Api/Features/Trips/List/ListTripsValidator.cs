namespace Mdsweep.Api.Features.Trips.List;

public sealed class ListTripsRequestValidator : AbstractValidator<ListTripsRequest>
{
    public ListTripsRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);

        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);

        RuleFor(x => x.SortBy).IsInEnum();

        RuleFor(x => x.SortDirection).IsInEnum();
    }
}
