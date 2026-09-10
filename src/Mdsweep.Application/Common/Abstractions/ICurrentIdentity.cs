namespace Mdsweep.Application.Common.Abstractions;

public interface ICurrentIdentity
{
    string Subject { get; }
    string? Email { get; }
    bool EmailVerified { get; }
}
