using Mdsweep.Domain.Passengers;
using Mdsweep.Domain.TripImports;
using Mdsweep.Domain.Trips;

namespace Mdsweep.Application.TripImports.Abstractions;

public interface ITripImportWorkflowStore
{
    Task<bool> HasContentFingerprintAsync(string contentFingerprint, CancellationToken ct);
    Task AddAsync(TripImportAggregate tripImport, CancellationToken ct);
    Task<TripImportAggregate?> FindImportAsync(Guid tripImportId, CancellationToken ct);
    Task<PassengerAggregate?> FindPassengerByBrokerMemberIdAsync(string brokerMemberId, CancellationToken ct);
    Task<TripAggregate?> FindTripByBrokerTripNumberAsync(string brokerTripNumber, CancellationToken ct);
    Task AddPassengerAsync(PassengerAggregate passenger, CancellationToken ct);
    Task AddTripAsync(TripAggregate trip, CancellationToken ct);
}
