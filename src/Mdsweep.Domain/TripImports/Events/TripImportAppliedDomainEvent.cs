using Mdsweep.Domain.Common.Abstractions;

namespace Mdsweep.Domain.TripImports.Events;

public sealed record TripImportAppliedDomainEvent(Guid TripImportId, Instant AppliedAt) : DomainEvent;
