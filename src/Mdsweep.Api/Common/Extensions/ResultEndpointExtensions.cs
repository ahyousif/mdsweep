namespace Mdsweep.Api.Common.Extensions;

internal static class ResultEndpointExtensions
{
    public static IResult ToEndpointResult(this Result result) =>
        result.Status switch
        {
            ResultStatus.Ok => Results.NoContent(),
            ResultStatus.NotFound => Results.NotFound(),
            ResultStatus.Conflict => Results.Conflict(),
            ResultStatus.Invalid => Results.ValidationProblem(ToValidationDictionary(result.ValidationErrors)),
            ResultStatus.Unauthorized => Results.Unauthorized(),
            ResultStatus.Forbidden => Results.Forbid(),
            _ => Results.BadRequest(),
        };

    public static IResult ToEndpointResult<TValue, TResponse>(
        this Result<TValue> result,
        Func<TValue, TResponse> map
    ) =>
        result.Status switch
        {
            ResultStatus.Ok => Results.Ok(map(result.Value)),
            ResultStatus.NotFound => Results.NotFound(),
            ResultStatus.Conflict => Results.Conflict(),
            ResultStatus.Invalid => Results.ValidationProblem(ToValidationDictionary(result.ValidationErrors)),
            ResultStatus.Unauthorized => Results.Unauthorized(),
            ResultStatus.Forbidden => Results.Forbid(),
            _ => Results.BadRequest(),
        };

    public static IResult ToEndpointResult<TValue>(this Result<TValue> result, Func<TValue, IResult> ok) =>
        result.Status switch
        {
            ResultStatus.Ok => ok(result.Value),
            ResultStatus.NotFound => Results.NotFound(),
            ResultStatus.Conflict => Results.Conflict(),
            ResultStatus.Invalid => Results.ValidationProblem(ToValidationDictionary(result.ValidationErrors)),
            ResultStatus.Unauthorized => Results.Unauthorized(),
            ResultStatus.Forbidden => Results.Forbid(),
            _ => Results.BadRequest(),
        };

    public static async Task<IResult> ToEndpointResultAsync<TValue>(
        this Result<TValue> result,
        Func<TValue, Task<IResult>> ok
    ) =>
        result.Status switch
        {
            ResultStatus.Ok => await ok(result.Value),
            ResultStatus.NotFound => Results.NotFound(),
            ResultStatus.Conflict => Results.Conflict(),
            ResultStatus.Invalid => Results.ValidationProblem(ToValidationDictionary(result.ValidationErrors)),
            ResultStatus.Unauthorized => Results.Unauthorized(),
            ResultStatus.Forbidden => Results.Forbid(),
            _ => Results.BadRequest(),
        };

    public static IResult ToEndpointResult<TValue, TResponse>(
        this PagedResult<IReadOnlyList<TValue>> result,
        Func<TValue, TResponse> map
    ) =>
        result.Status switch
        {
            ResultStatus.Ok => Results.Ok(
                new
                {
                    Items = result.Value.Select(map).ToList(),

                    TotalCount = result.PagedInfo.TotalRecords,
                    Page = result.PagedInfo.PageNumber,
                    PageSize = result.PagedInfo.PageSize,
                    TotalPages = result.PagedInfo.TotalPages,
                }
            ),

            ResultStatus.NotFound => Results.NotFound(),
            ResultStatus.Conflict => Results.Conflict(),
            ResultStatus.Invalid => Results.ValidationProblem(ToValidationDictionary(result.ValidationErrors)),
            ResultStatus.Unauthorized => Results.Unauthorized(),
            ResultStatus.Forbidden => Results.Forbid(),
            _ => Results.BadRequest(),
        };

    private static Dictionary<string, string[]> ToValidationDictionary(IEnumerable<ValidationError> errors) =>
        errors
            .GroupBy(error => error.Identifier)
            .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray());
}
