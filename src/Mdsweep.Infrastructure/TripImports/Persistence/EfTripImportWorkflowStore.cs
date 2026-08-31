using Mdsweep.Application.TripImports.Abstractions;
using Mdsweep.Domain.Passengers;
using Mdsweep.Domain.TripImports;
using Mdsweep.Domain.Trips;
using Mdsweep.Infrastructure.Persistence;

namespace Mdsweep.Infrastructure.TripImports.Persistence;

public sealed class EfTripImportWorkflowStore(ApplicationDbContext db) : ITripImportWorkflowStore
{
    public Task<bool> HasContentFingerprintAsync(string contentFingerprint, CancellationToken ct) =>
        db.TripImports.AnyAsync(import => import.ContentFingerprint == contentFingerprint, ct);

    public Task AddAsync(TripImportAggregate tripImport, CancellationToken ct) =>
        db.TripImports.AddAsync(tripImport, ct).AsTask();

    public Task<TripImportAggregate?> FindImportAsync(Guid tripImportId, CancellationToken ct) =>
        db.TripImports.Include(import => import.Rows).SingleOrDefaultAsync(import => import.Id == tripImportId, ct);

    public Task<PassengerAggregate?> FindPassengerByBrokerMemberIdAsync(string brokerMemberId, CancellationToken ct) =>
        db.Passengers.SingleOrDefaultAsync(passenger => passenger.BrokerMemberId == brokerMemberId, ct);

    public Task<TripAggregate?> FindTripByBrokerTripNumberAsync(string brokerTripNumber, CancellationToken ct) =>
        db.Trips.SingleOrDefaultAsync(trip => trip.BrokerTripNumber == brokerTripNumber, ct);

    public Task AddPassengerAsync(PassengerAggregate passenger, CancellationToken ct) =>
        db.Passengers.AddAsync(passenger, ct).AsTask();

    public Task AddTripAsync(TripAggregate trip, CancellationToken ct) =>
        db.Trips.AddAsync(trip, ct).AsTask();
}
