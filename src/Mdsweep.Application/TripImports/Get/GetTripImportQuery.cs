using Mdsweep.Application.Common.Abstractions;

namespace Mdsweep.Application.TripImports.Get;

public sealed record GetTripImportQuery(Guid Id) : IQuery<TripImportModel>;
