using Mdsweep.Application.Dispatch;

namespace Mdsweep.Api.Features.Dispatch;

internal static class DispatchHttpResult
{
    public static IResult Map<T>(
        DispatchManagementResult<T> result,
        Func<DispatchManagementResult<T>, IResult> success
    ) =>
        result.Outcome switch
        {
            DispatchManagementOutcome.Success => success(result),
            DispatchManagementOutcome.NotFound => Results.NotFound(),
            DispatchManagementOutcome.Conflict => Results.Conflict(
                new { message = result.Message }
            ),
            _ => Results.BadRequest(new { message = result.Message }),
        };
}
