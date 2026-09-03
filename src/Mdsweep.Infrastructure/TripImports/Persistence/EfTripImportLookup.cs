using Mdsweep.Application.TripImports.Abstractions;
using Mdsweep.Domain.Passengers;
using Mdsweep.Domain.TripImports;
using Mdsweep.Domain.Trips;
using Mdsweep.Infrastructure.Persistence;

namespace Mdsweep.Infrastructure.TripImports.Persistence;

public sealed class EfTripImportLookup(ApplicationDbContext db) : ITripImportLookup
{
    public Task<bool> HasAppliedImportAsync(string contentFingerprint, CancellationToken ct) =>
        db.TripImports.AnyAsync(import => import.ContentFingerprint == contentFingerprint && import.Status == TripImportStatus.Applied, ct);
    public Task<TripImportAggregate?> FindImportAsync(Guid tripImportId, CancellationToken ct) =>
        db.TripImports.Include(import => import.Items).SingleOrDefaultAsync(import => import.Id == tripImportId, ct);

    public async Task<IReadOnlyList<PassengerAggregate>> FindPassengersAsync(
        IReadOnlyCollection<string> brokerMemberIds, CancellationToken ct
    )
    {
        var normalizedIds = brokerMemberIds.Select(id => id.ToUpperInvariant()).ToArray();
        return await db.Passengers.Where(passenger => normalizedIds.Contains(passenger.BrokerMemberId!)).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<TripAggregate>> FindTripsAsync(
        IReadOnlyCollection<string> brokerTripNumbers, CancellationToken ct
    )
    {
        var normalizedNumbers = brokerTripNumbers.Select(number => number.ToUpperInvariant()).ToArray();
        return await db.Trips.Where(trip => normalizedNumbers.Contains(trip.BrokerTripNumber)).ToListAsync(ct);
    }
}
