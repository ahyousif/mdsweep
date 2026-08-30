namespace Mdsweep.Application.Dispatch;

public sealed record DispatchManagementResult<T>(
    DispatchManagementOutcome Outcome,
    T? Value = default,
    string? Message = null,
    string? Location = null
);
