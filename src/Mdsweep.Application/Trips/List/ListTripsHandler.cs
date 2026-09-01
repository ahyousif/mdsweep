using Mdsweep.Application.Common.Pagination;
using Mdsweep.Application.Common.Persistence;
using ApplicationPagedResult = Mdsweep.Application.Common.Pagination.PagedResult<Mdsweep.Application.Trips.TripModel>;

namespace Mdsweep.Application.Trips.List;

public sealed class ListTripsHandler(IRepository repository)
{
    public async Task<Result<ApplicationPagedResult>> Handle(ListTripsQuery query, CancellationToken ct)
    {
        var validationErrors = Validate(query);
        if (validationErrors.Count > 0)
        {
            return Result.Invalid(validationErrors);
        }

        var totalCount = await repository.CountAsync(new CountTripsSpecification(query), ct);
        var items = await repository.ListAsync(new ListTripsSpecification(query), ct);

        return Result.Success(new ApplicationPagedResult(items, totalCount, query.Page, query.PageSize));
    }

    private static List<ValidationError> Validate(ListTripsQuery query)
    {
        var validationErrors = new List<ValidationError>();

        if (query.Page < 1)
        {
            validationErrors.Add(
                new ValidationError { Identifier = "page", ErrorMessage = "Page must be at least 1." }
            );
        }

        if (query.PageSize is < 1 or > 100)
        {
            validationErrors.Add(
                new ValidationError { Identifier = "pageSize", ErrorMessage = "Page size must be between 1 and 100." }
            );
        }

        if (query.PageSize > 0 && query.Page > int.MaxValue / query.PageSize)
        {
            validationErrors.Add(new ValidationError { Identifier = "page", ErrorMessage = "Page is too large." });
        }

        if (!Enum.IsDefined(query.SortBy))
        {
            validationErrors.Add(
                new ValidationError { Identifier = "sortBy", ErrorMessage = "Sort by is not supported." }
            );
        }

        if (!Enum.IsDefined(query.SortDirection))
        {
            validationErrors.Add(
                new ValidationError { Identifier = "sortDirection", ErrorMessage = "Sort direction is not supported." }
            );
        }

        return validationErrors;
    }
}
