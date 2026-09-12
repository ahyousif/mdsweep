using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Http.FluentValidation;

namespace Mdsweep.Api.Common.Extensions;

// Keep the existing validation shape and add machine-readable codes for the SPA.
public sealed class LocalizedValidationProblemSource<T> : IProblemDetailSource<T>
{
    public ProblemDetails Create(T message, IReadOnlyList<ValidationFailure> failures)
    {
        var problem = new ValidationProblemDetails(
            failures.GroupBy(f => f.PropertyName).ToDictionary(g => g.Key, g => g.Select(f => f.ErrorMessage).ToArray())
        )
        {
            Status = StatusCodes.Status400BadRequest,
        };
        problem.Extensions["localizedErrors"] = failures
            .Select(f => new { field = f.PropertyName, code = f.ErrorCode })
            .ToArray();
        return problem;
    }
}
