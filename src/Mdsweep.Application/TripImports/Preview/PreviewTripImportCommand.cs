using Mdsweep.Application.Common.Abstractions;

namespace Mdsweep.Application.TripImports.Preview;

public sealed record PreviewTripImportCommand(string FileName, string? ContentType, byte[] Content) : ICommand<Guid>;
