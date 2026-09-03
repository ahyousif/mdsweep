using Mdsweep.Domain.Common.Abstractions;

namespace Mdsweep.Domain.TripImports.Events;

public sealed record TripImportPreviewedDomainEvent(Guid TripImportId) : DomainEvent;
