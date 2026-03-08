using Aimy.Core.Application.Configuration;
using Aimy.Core.Application.DTOs;
using Aimy.Core.Application.DTOs.KnowledgeBase;
using Aimy.Core.Application.Interfaces.KnowledgeBase;
using Aimy.Core.Domain.Entities;
using Microsoft.Extensions.Options;


namespace Aimy.Core.Application.Services;

public class SemanticSearchService(
    IVectorSearchPort vectorSearchPort,
    IOptions<SemanticSearchOptions> options,
    IKnowledgeItemRepository knowledgeItemRepository) : ISemanticSearchService
{
    public async Task<PagedResult<SemanticSearchResultResponse>> SearchAsync(string query, int page, int pageSize, CancellationToken ct)
    {
        var semanticSearchOptions = options.Value;
        var vectorResults = await vectorSearchPort.SearchAsync(
            query,
            semanticSearchOptions.MaxResults,
            semanticSearchOptions.ScoreThreshold,
            ct);

        var sourceUploadIds = vectorResults
            .Select(result => Guid.TryParse(result.SourceId, out var sourceUploadId) ? sourceUploadId : (Guid?)null)
            .Where(sourceUploadId => sourceUploadId.HasValue)
            .Select(sourceUploadId => sourceUploadId!.Value)
            .Distinct()
            .ToList();

        var knowledgeItems = await knowledgeItemRepository.GetBySourceUploadIdsAsync(sourceUploadIds, ct);

        var knowledgeItemsBySourceUploadId = knowledgeItems
            .Where(item => item.SourceUploadId.HasValue)
            .GroupBy(item => item.SourceUploadId!.Value)
            .ToDictionary(group => group.Key, group => group.ToList());

        var allResults = vectorResults
            .SelectMany(result =>
            {
                if (!Guid.TryParse(result.SourceId, out var sourceUploadId))
                {
                    return Enumerable.Empty<SemanticSearchResultResponse>();
                }

                if (!knowledgeItemsBySourceUploadId.TryGetValue(sourceUploadId, out var items))
                {
                    return Enumerable.Empty<SemanticSearchResultResponse>();
                }

                return items.Select(item => new SemanticSearchResultResponse
                {
                    ItemResponse = MapToResponse(item),
                    Score = result.Score ?? 0
                });
            })
            .OrderByDescending(result => result.Score)
            .ToList();

        var totalCount = allResults.Count;
        var pagedItems = allResults
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<SemanticSearchResultResponse>
        {
            Items = pagedItems,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    private static ItemResponse MapToResponse(KnowledgeItem item)
    {
        return new ItemResponse
        {
            Id = item.Id,
            FolderId = item.FolderId,
            FolderName = item.Folder?.Name,
            Title = item.Title,
            ItemType = item.ItemType,
            Content = item.Content,
            Metadata = item.Metadata,
            SourceUploadId = item.SourceUploadId,
            SourceUploadFileName = item.SourceUpload?.FileName,
            SourceUploadMetadata = item.SourceUpload?.Metadata,
            SourceMarkdown = item.SourceUpload?.SourceMarkdown,
            CreatedAt = item.CreatedAt,
        };
    }
}
