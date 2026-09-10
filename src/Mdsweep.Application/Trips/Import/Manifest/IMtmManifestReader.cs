namespace Mdsweep.Application.Trips.Import.Manifest;

public interface IMtmManifestReader
{
    Task<MtmManifestReadResult> ReadAsync(string fileName, ReadOnlyMemory<byte> content, CancellationToken ct);
}
