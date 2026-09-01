using FluentValidation;

namespace Mdsweep.Api.Features.Trips.List;

public sealed class ListTripsValidator : AbstractValidator<ListTripsRequest>
{
    public ListTripsValidator()
    {
        RuleFor(request => request.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page must be at least 1.")
            .OverridePropertyName("page");
        RuleFor(request => request.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("Page size must be between 1 and 100.")
            .OverridePropertyName("pageSize");
        RuleFor(request => request.SortBy)
            .IsInEnum()
            .WithMessage("Sort by is not supported.")
            .OverridePropertyName("sortBy");
        RuleFor(request => request.SortDirection)
            .IsInEnum()
            .WithMessage("Sort direction is not supported.")
            .OverridePropertyName("sortDirection");
        RuleFor(request => request)
            .Must(request => request.HasValidServiceDate())
            .WithMessage("Service date must use ISO-8601 format (yyyy-MM-dd).")
            .OverridePropertyName("serviceDate");
    }
}
