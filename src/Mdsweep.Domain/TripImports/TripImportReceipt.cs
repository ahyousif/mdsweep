using Mdsweep.Domain.Common.Abstractions;

namespace Mdsweep.Domain.TripImports;

public sealed class TripImportReceipt : AggregateRoot<Guid>, ITenanted
{
    private TripImportReceipt() : base(default) { }

    private TripImportReceipt(Guid id, string fileName, string contentHash, int total, int added, int updated, int unchanged, int problemCount, Instant importedAt)
        : base(id)
    {
        FileName = fileName;
        ContentHash = contentHash;
        Total = total;
        Added = added;
        Updated = updated;
        Unchanged = unchanged;
        ProblemCount = problemCount;
        ImportedAt = importedAt;
    }

    public string? TenantId { get; set; }
    public string FileName { get; private set; } = null!;
    public string ContentHash { get; private set; } = null!;
    public int Total { get; private set; }
    public int Added { get; private set; }
    public int Updated { get; private set; }
    public int Unchanged { get; private set; }
    public int ProblemCount { get; private set; }
    public Instant ImportedAt { get; private set; }

    public static TripImportReceipt Create(string fileName, string contentHash, ImportOutcome outcome, Instant importedAt) =>
        new(Guid.CreateVersion7(), fileName, contentHash, outcome.Total, outcome.Added, outcome.Updated, outcome.Unchanged, outcome.ProblemCount, importedAt);
}

public sealed record ImportOutcome(int Total, int Added, int Updated, int Unchanged, int ProblemCount);
