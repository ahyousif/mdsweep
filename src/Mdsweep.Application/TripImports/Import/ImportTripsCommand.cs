using Mdsweep.Application.Common.Abstractions;

namespace Mdsweep.Application.TripImports.Import;

public sealed record ImportTripsCommand(string FileName, string? ContentType, byte[] Content)
    : ICommand<ImportTripsResult>;
