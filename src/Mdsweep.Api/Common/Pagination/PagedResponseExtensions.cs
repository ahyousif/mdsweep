using Mdsweep.Application.Common.Pagination;

namespace Mdsweep.Api.Common.Pagination;

public static class PagedResponseExtensions
{
    public static PagedResponse<TResponse> ToResponse<TModel, TResponse>(
        this PagedResult<TModel> result,
        Func<TModel, TResponse> map
    ) => new([.. result.Items.Select(map)], result.TotalCount, result.Page, result.PageSize);
}
