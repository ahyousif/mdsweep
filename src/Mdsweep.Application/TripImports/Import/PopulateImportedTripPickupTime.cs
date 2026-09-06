namespace Mdsweep.Application.TripImports.Import;

/// <summary>
/// Durable post-import enrichment for one imported or reconciled trip.
/// </summary>
public sealed record PopulateImportedTripPickupTime(Guid TripId);
