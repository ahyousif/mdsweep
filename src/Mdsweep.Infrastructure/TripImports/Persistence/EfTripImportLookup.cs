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
        db.TripImports.Include(import => import.Rows).SingleOrDefaultAsync(import => import.Id == tripImportId, ct);

    public async Task<IReadOnlyList<PassengerAggregate>> FindPassengersAsync(
        IReadOnlyCollection<string> brokerMemberIds, CancellationToken ct
    ) => await db.Passengers.Where(passenger => brokerMemberIds.Contains(passenger.BrokerMemberId!)).ToListAsync(ct);

    public async Task<IReadOnlyList<TripAggregate>> FindTripsAsync(
        IReadOnlyCollection<string> brokerTripNumbers, CancellationToken ct
    ) => await db.Trips.Where(trip => brokerTripNumbers.Contains(trip.BrokerTripNumber)).ToListAsync(ct);
}
