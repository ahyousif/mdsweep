using EndpointResult = Microsoft.AspNetCore.Http.IResult;

namespace Mdsweep.Api.Common.Extensions;

internal static class ResultEndpointExtensions
{
    public static EndpointResult ToEndpointResult(this ArdalisResult.Result result) =>
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

    public static EndpointResult ToEndpointResult<TValue, TResponse>(
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

    public static EndpointResult ToEndpointResult<TValue>(
        this Result<TValue> result,
        Func<TValue, EndpointResult> ok
    ) =>
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

    public static async Task<EndpointResult> ToEndpointResultAsync<TValue>(
        this Result<TValue> result,
        Func<TValue, Task<EndpointResult>> ok
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

    public static EndpointResult ToEndpointResult<TValue, TResponse>(
        this PagedResult<IReadOnlyList<TValue>> result,
        Func<TValue, TResponse> map
    ) =>
        result.Status switch
        {
            ResultStatus.Ok => Results.Ok(
                new
                {
                    Items = result.Value.Select(map),
                    result.PagedInfo.PageNumber,
                    result.PagedInfo.PageSize,
                    result.PagedInfo.TotalPages,
                    result.PagedInfo.TotalRecords,
                }
            ),

            ResultStatus.NotFound => Results.NotFound(),
            ResultStatus.Invalid => Results.ValidationProblem(ToValidationDictionary(result.ValidationErrors)),
            _ => Results.BadRequest(),
        };

    private static Dictionary<string, string[]> ToValidationDictionary(IEnumerable<ValidationError> errors) =>
        errors
            .GroupBy(error => error.Identifier)
            .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray());
}
