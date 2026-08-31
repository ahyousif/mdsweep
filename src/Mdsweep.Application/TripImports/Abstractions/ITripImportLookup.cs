using Mdsweep.Domain.Passengers;
using Mdsweep.Domain.TripImports;
using Mdsweep.Domain.Trips;

namespace Mdsweep.Application.TripImports.Abstractions;

public interface ITripImportLookup
{
    Task<TripImportAggregate?> FindImportAsync(Guid tripImportId, CancellationToken ct);
    Task<IReadOnlyList<PassengerAggregate>> FindPassengersAsync(
        IReadOnlyCollection<string> brokerMemberIds,
        CancellationToken ct
    );
    Task<IReadOnlyList<TripAggregate>> FindTripsAsync(
        IReadOnlyCollection<string> brokerTripNumbers,
        CancellationToken ct
    );
}
