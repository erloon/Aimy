namespace Aimy.Core.Domain.Entities;

public class IngestionEmbedding
{
    public Guid Id { get; set; }
    public required string SourceId { get; set; }
    public required string Content { get; set; }
    public required float[] Embedding { get; set; }
    public string? Metadata { get; set; }
    public DateTime CreatedAt { get; set; }
}
