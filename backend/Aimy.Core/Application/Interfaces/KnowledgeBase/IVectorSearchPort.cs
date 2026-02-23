namespace Aimy.Core.Application.Interfaces.KnowledgeBase;

public record VectorSearchResult(string SourceId, double Score);

public interface IVectorSearchPort
{
    Task<IReadOnlyList<VectorSearchResult>> SearchAsync(string query, int maxResults, double scoreThreshold, CancellationToken ct);
}
