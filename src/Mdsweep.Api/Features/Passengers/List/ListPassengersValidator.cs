namespace Mdsweep.Api.Features.Passengers.List;

public sealed class ListPassengersRequestValidator : AbstractValidator<ListPassengersRequest>
{
    public ListPassengersRequestValidator()
    {
        RuleFor(request => request.Page).GreaterThanOrEqualTo(1).OverridePropertyName("page");
        RuleFor(request => request.PageSize).InclusiveBetween(1, 100).OverridePropertyName("pageSize");
        RuleFor(request => request.Search).MaximumLength(200).OverridePropertyName("search");
    }
}
