namespace Mdsweep.Api.Common.Pagination;

public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);
