namespace Mdsweep.Application.Dispatch;

public sealed record CreateDriverAccessRequest(
    string Email,
    string TemporaryPassword,
    string DisplayName,
    string MtmDriverNumber
);
