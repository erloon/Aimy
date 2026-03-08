using Aimy.Core.Application.Interfaces.KnowledgeBase;

namespace Aimy.Infrastructure.Repositories;

using Aimy.Core.Application.DTOs;
using Aimy.Core.Domain.Entities;
using Aimy.Infrastructure.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

public class KnowledgeItemRepository(ApplicationDbContext context) : IKnowledgeItemRepository
{
    public async Task<KnowledgeItem> AddAsync(KnowledgeItem item, CancellationToken ct)
    {
        context.KnowledgeItems.Add(item);
        await context.SaveChangesAsync(ct);
        return item;
    }

    public async Task<KnowledgeItem?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await context.KnowledgeItems
            .Include(ki => ki.Folder!)
            .ThenInclude(f => f!.KnowledgeBase)
            .Include(ki => ki.SourceUpload)
            .FirstOrDefaultAsync(ki => ki.Id == id, ct);
    }

    public async Task<IEnumerable<KnowledgeItem>> GetByFolderIdAsync(Guid folderId, CancellationToken ct)
    {
        return await context.KnowledgeItems
            .Include(ki => ki.SourceUpload)
            .Where(ki => ki.FolderId == folderId)
            .OrderByDescending(ki => ki.UpdatedAt)
            .ToListAsync(ct);
    }

    public async Task<PagedResult<KnowledgeItem>> SearchAsync(
        Guid knowledgeBaseId,
        Guid? folderId,
        IReadOnlyCollection<Guid>? folderIds,
        string? search,
        string? metadata,
        KnowledgeItemType? type,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var query = context.KnowledgeItems
            .Include(ki => ki.Folder!)
            .ThenInclude(f => f.KnowledgeBase)
            .Include(ki => ki.SourceUpload)
            .Where(ki => ki.Folder!.KnowledgeBaseId == knowledgeBaseId);

        if (folderIds is { Count: > 0 })
        {
            query = query.Where(ki => folderIds.Contains(ki.FolderId));
        }
        else if (folderId.HasValue)
        {
            query = query.Where(ki => ki.FolderId == folderId.Value);
        }
        
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(ki => 
                ki.Title.Contains(search) || 
                (ki.Content != null && ki.Content.Contains(search)));
        }
        
        if (type.HasValue)
        {
            query = query.Where(ki => ki.ItemType == type.Value);
        }

        var metadataQuery = metadata?.Trim();
        var metadataKey = default(string);
        var metadataValue = default(string);

        if (!string.IsNullOrWhiteSpace(metadataQuery))
        {
            var separatorIndex = metadataQuery.IndexOf(':');

            if (separatorIndex > 0 && separatorIndex < metadataQuery.Length - 1)
            {
                var rawKey = metadataQuery[..separatorIndex].Trim();
                var rawValue = metadataQuery[(separatorIndex + 1)..].Trim();

                var key = TrimOptionalQuotes(rawKey);
                var value = TrimOptionalQuotes(rawValue);

                if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
                {
                    metadataKey = key;
                    metadataValue = value;
                }
            }
        }

        List<KnowledgeItem> items;
        var totalCount = 0;

        if (!string.IsNullOrWhiteSpace(metadataQuery))
        {
            var candidates = await query
                .OrderByDescending(ki => ki.UpdatedAt)
                .ToListAsync(ct);

            var filtered = candidates
                .Where(item => MetadataMatches(item.Metadata, metadataKey, metadataValue, metadataQuery))
                .ToList();

            totalCount = filtered.Count;
            items = filtered
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }
        else
        {
            totalCount = await query.CountAsync(ct);
            items = await query
                .OrderByDescending(ki => ki.UpdatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);
        }
        
        return new PagedResult<KnowledgeItem>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<bool> ExistsBySourceUploadIdAsync(Guid sourceUploadId, CancellationToken ct)
    {
        return await context.KnowledgeItems
            .AnyAsync(ki => ki.SourceUploadId == sourceUploadId, ct);
    }

    public async Task<IReadOnlyCollection<KnowledgeItem>> GetBySourceUploadIdAsync(Guid sourceUploadId, CancellationToken ct)
    {
        return await context.KnowledgeItems
            .Where(ki => ki.SourceUploadId == sourceUploadId)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<KnowledgeItem>> GetBySourceUploadIdsAsync(IReadOnlyCollection<Guid> sourceUploadIds, CancellationToken ct)
    {
        return await context.KnowledgeItems
            .Include(ki => ki.Folder!)
            .ThenInclude(f => f!.KnowledgeBase)
            .Include(ki => ki.SourceUpload)
            .Where(ki => ki.SourceUploadId.HasValue && sourceUploadIds.Contains(ki.SourceUploadId!.Value))
            .ToListAsync(ct);
    }

    public async Task UpdateAsync(KnowledgeItem item, CancellationToken ct)
    {
        context.KnowledgeItems.Update(item);
        await context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var item = await context.KnowledgeItems.FindAsync([id], ct);
        if (item != null)
        {
            context.KnowledgeItems.Remove(item);
            await context.SaveChangesAsync(ct);
        }
    }

    private static string TrimOptionalQuotes(string input)
    {
        if (input.Length >= 2)
        {
            if ((input.StartsWith('"') && input.EndsWith('"'))
                || (input.StartsWith('\'') && input.EndsWith('\'')))
            {
                return input[1..^1].Trim();
            }
        }

        return input;
    }

    private static bool MetadataMatches(string? rawMetadata, string? key, string? value, string query)
    {
        if (string.IsNullOrWhiteSpace(rawMetadata))
        {
            return false;
        }

        if (TryParseMetadataObject(rawMetadata, out var rootElement))
        {
            if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
            {
                foreach (var property in rootElement.EnumerateObject())
                {
                    if (!string.Equals(property.Name, key, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var propertyValue = ExtractPropertyValue(property.Value);
                    return propertyValue.Contains(value, StringComparison.OrdinalIgnoreCase);
                }

                return false;
            }

            var normalizedQuery = TrimOptionalQuotes(query).Trim();
            if (string.IsNullOrWhiteSpace(normalizedQuery))
            {
                return true;
            }

            foreach (var property in rootElement.EnumerateObject())
            {
                if (property.Name.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                var propertyValue = ExtractPropertyValue(property.Value);
                if (propertyValue.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        var fallbackQuery = TrimOptionalQuotes(query).Trim();
        return rawMetadata.Contains(fallbackQuery, StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryParseMetadataObject(string rawMetadata, out JsonElement rootElement)
    {
        try
        {
            using var document = JsonDocument.Parse(rawMetadata);
            if (document.RootElement.ValueKind == JsonValueKind.Object)
            {
                rootElement = document.RootElement.Clone();
                return true;
            }
        }
        catch (JsonException)
        {
            // fall through
        }

        rootElement = default;
        return false;
    }

    private static string ExtractPropertyValue(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? string.Empty,
            JsonValueKind.Null => string.Empty,
            _ => value.GetRawText()
        };
    }
}
