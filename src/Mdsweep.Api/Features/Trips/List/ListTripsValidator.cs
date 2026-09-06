namespace Mdsweep.Api.Features.Trips.List;

public sealed class ListTripsRequestValidator : AbstractValidator<ListTripsRequest>
{
    public ListTripsRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).OverridePropertyName("page");

        RuleFor(x => x.PageSize).InclusiveBetween(1, 100).OverridePropertyName("pageSize");

        RuleFor(x => x.Search).MaximumLength(200).OverridePropertyName("search");

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
            .OverridePropertyName("endDate");

        RuleFor(x => x.SortBy).IsInEnum().OverridePropertyName("sortBy");

        RuleFor(x => x.SortDirection).IsInEnum().OverridePropertyName("sortDirection");
    }
}
