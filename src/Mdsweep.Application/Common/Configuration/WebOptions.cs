namespace Mdsweep.Application.Common.Configuration;

public sealed class WebOptions
{
    public const string SectionName = "Web";

    public required string BaseUrl { get; init; }
}
