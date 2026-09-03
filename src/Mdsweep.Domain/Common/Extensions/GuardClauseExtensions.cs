namespace Mdsweep.Domain.Common.Extensions;

public static class GuardClauseExtensions
{
    public static void Invalid(this IGuardClause guardClause, bool condition, string? message = null)
    {
        if (condition)
        {
            throw new InvalidOperationException(message ?? "Condition is invalid.");
        }
    }

    public static T NotFound<T>(this IGuardClause guardClause, T? input, string? message = null)
        where T : class
    {
        if (input is null)
        {
            throw new InvalidOperationException(message ?? "Entity not found.");
        }

        return input;
    }
}
