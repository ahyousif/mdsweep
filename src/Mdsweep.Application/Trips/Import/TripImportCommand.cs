using Mdsweep.Application.Common.Abstractions;

namespace Mdsweep.Application.Trips.Import;

public sealed record TripImportCommand(string FileName, byte[] Content) : ICommand<TripImportSummary>;
