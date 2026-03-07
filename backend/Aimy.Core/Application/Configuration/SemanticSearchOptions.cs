namespace Aimy.Core.Application.Configuration;

public sealed class SemanticSearchOptions
{
    public const string SectionName = "SemanticSearch";

    public int MaxResults { get; set; } = 50;

    public double ScoreThreshold { get; set; } = 0.35;

    public int DefaultPageSize { get; set; } = 10;
}
