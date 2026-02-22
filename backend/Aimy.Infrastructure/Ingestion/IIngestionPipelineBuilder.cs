using Aimy.Core.Domain.Entities;

namespace Aimy.Infrastructure.Ingestion;

public interface IIngestionPipelineBuilder
{
    Task<IngestionPipelineComponents> BuildAsync(Upload upload, CancellationToken cancellationToken);
}
