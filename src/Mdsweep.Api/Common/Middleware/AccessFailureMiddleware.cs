using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Mdsweep.Api.Common.Middleware;

public sealed class AccessFailureMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (AntiforgeryValidationException) when (IsAccess(context))
        {
            await Results
                .Problem("Your session needs refreshing. Reload the page and try again.", statusCode: 400)
                .ExecuteAsync(context);
        }
        catch (DbUpdateConcurrencyException) when (IsAccess(context))
        {
            await Conflict(context);
        }
        catch (DbUpdateException exception)
            when (IsAccess(context)
                && exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }
            )
        {
            await Conflict(context);
        }
    }

    private static bool IsAccess(HttpContext context) =>
        context.Request.Path.StartsWithSegments("/api/users")
        || context.Request.Path.StartsWithSegments("/api/invitations");

    private static Task Conflict(HttpContext context) =>
        Results
            .Problem(
                "Access changed or this email already has a User or invitation. Refresh the list before trying again.",
                statusCode: 409
            )
            .ExecuteAsync(context);
}
