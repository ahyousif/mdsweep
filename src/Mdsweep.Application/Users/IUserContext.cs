namespace Mdsweep.Application.Users;

// Access also runs before Tenant selection. Identity claims are supplied by the BFF,
// never by request bodies; application authorization remains database-backed.
public interface IUserContext
{
    string Subject { get; }
    string? TenantId { get; }
}
