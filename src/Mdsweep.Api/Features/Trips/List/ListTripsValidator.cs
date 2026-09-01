namespace Mdsweep.Api.Features.Trips.List;

public sealed class ListTripsRequestValidator : AbstractValidator<ListTripsRequest>
{
    public ListTripsRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).OverridePropertyName("page");

        RuleFor(x => x.PageSize).InclusiveBetween(1, 100).OverridePropertyName("pageSize");

        RuleFor(x => x.SortBy).IsInEnum().OverridePropertyName("sortBy");

        RuleFor(x => x.SortDirection).IsInEnum().OverridePropertyName("sortDirection");
    }
}
