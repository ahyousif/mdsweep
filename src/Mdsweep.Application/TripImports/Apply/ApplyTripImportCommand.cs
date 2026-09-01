using Mdsweep.Application.Common.Abstractions;

namespace Mdsweep.Application.TripImports.Apply;

public sealed record ApplyTripImportCommand(Guid Id) : ICommand<TripImportModel>;
