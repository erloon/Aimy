using System.Text;
using Microsoft.Extensions.DataIngestion;

namespace Aimy.Infrastructure.Ingestion;

public sealed class RawMarkdownReader : IngestionDocumentReader
{
    public override async Task<IngestionDocument> ReadAsync(
        Stream source,
        string identifier,
        string mediaType,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);

        using var reader = new StreamReader(source, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var markdown = await reader.ReadToEndAsync(cancellationToken);

        var document = new IngestionDocument(identifier);
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return document;
        }

        var section = new IngestionDocumentSection();
        section.Elements.Add(new IngestionDocumentParagraph(markdown));
        document.Sections.Add(section);

        return document;
    }
}
