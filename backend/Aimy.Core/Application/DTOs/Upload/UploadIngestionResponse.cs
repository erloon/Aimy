namespace Aimy.Core.Application.DTOs.Upload;

public class UploadIngestionResponse
{
    public string? Summary { get; set; }

    public int ChunkCount => Chunks.Count;

    public List<UploadChunkResponse> Chunks { get; set; } = [];
}

public class UploadChunkResponse
{
    public Guid Id { get; set; }

    public required string Content { get; set; }

    public string? Context { get; set; }

    public string? Summary { get; set; }

    public string? Metadata { get; set; }

    public DateTime CreatedAt { get; set; }
}
