namespace Aimy.Core.Domain.Entities;

public class Upload
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required string FileName { get; set; }
    public required string StoragePath { get; set; }
    public long FileSizeBytes { get; set; }
    public string? ContentType { get; set; }
    public string? Metadata { get; set; } // JSON string for key-value metadata
    public string? SourceMarkdown { get; set; }
    public DateTime DateUploaded { get; set; }

    public bool IsMarkdownUpload => HasMarkdownContentType() || HasMarkdownFileExtension();

    public Upload()
    {
        Id = Guid.NewGuid();
        DateUploaded = DateTime.UtcNow;
    }

    private bool HasMarkdownContentType()
    {
        if (string.IsNullOrWhiteSpace(ContentType))
        {
            return false;
        }

        var mediaType = ContentType.Split(';', 2)[0].Trim();
        return mediaType.Equals("text/markdown", StringComparison.OrdinalIgnoreCase)
            || mediaType.Equals("text/x-markdown", StringComparison.OrdinalIgnoreCase)
            || mediaType.Equals("application/markdown", StringComparison.OrdinalIgnoreCase)
            || mediaType.Equals("application/x-markdown", StringComparison.OrdinalIgnoreCase);
    }

    private bool HasMarkdownFileExtension()
    {
        var extension = Path.GetExtension(FileName);
        return extension.Equals(".md", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".markdown", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".mdown", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".mkd", StringComparison.OrdinalIgnoreCase);
    }
}
