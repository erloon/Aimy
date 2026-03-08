using Aimy.Core.Application.Interfaces.Metadata;
using Aimy.Core.Domain.Entities;
using Aimy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Aimy.Infrastructure.Repositories;

public class MetadataCatalogRepository(ApplicationDbContext context) : IMetadataCatalogRepository
{
    public async Task<IReadOnlyList<MetadataDefinition>> GetDefinitionsAsync(CancellationToken ct)
    {
        return await context.MetadataDefinitions
            .Where(definition => definition.IsActive)
            .OrderBy(definition => definition.Key)
            .ToListAsync(ct);
    }

    public async Task<MetadataDefinition?> GetDefinitionByKeyAsync(string key, CancellationToken ct)
    {
        var normalizedKey = key.Trim();
        return await context.MetadataDefinitions
            .FirstOrDefaultAsync(definition => definition.IsActive && definition.Key.ToLower() == normalizedKey.ToLower(), ct);
    }

    public async Task<IReadOnlyList<MetadataValueOption>> GetValueOptionsAsync(string key, string? prefix, CancellationToken ct)
    {
        var normalizedKey = key.Trim().ToLowerInvariant();

        var options = await context.MetadataValueOptions
            .Include(option => option.Definition)
            .Where(option => option.IsActive
                && option.Definition != null
                && option.Definition.IsActive
                && option.Definition.Key.ToLower() == normalizedKey)
            .OrderBy(option => option.SortOrder)
            .ThenBy(option => option.DisplayLabel)
            .ToListAsync(ct);

        if (string.IsNullOrWhiteSpace(prefix))
        {
            return options;
        }

        var normalizedPrefix = prefix.Trim();
        return options
            .Where(option =>
                option.CanonicalValue.StartsWith(normalizedPrefix, StringComparison.OrdinalIgnoreCase)
                || option.DisplayLabel.StartsWith(normalizedPrefix, StringComparison.OrdinalIgnoreCase)
                || option.Aliases.Any(alias => alias.StartsWith(normalizedPrefix, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    public async Task<IReadOnlyList<MetadataValueOption>> GetAllValueOptionsAsync(string key, CancellationToken ct)
    {
        var normalizedKey = key.Trim().ToLowerInvariant();
        return await context.MetadataValueOptions
            .Include(option => option.Definition)
            .Where(option => option.IsActive
                && option.Definition != null
                && option.Definition.IsActive
                && option.Definition.Key.ToLower() == normalizedKey)
            .OrderBy(option => option.SortOrder)
            .ThenBy(option => option.DisplayLabel)
            .ToListAsync(ct);
    }

    public async Task<MetadataDefinition> UpsertDefinitionAsync(MetadataDefinition definition, CancellationToken ct)
    {
        var key = definition.Key.Trim();
        var existing = await context.MetadataDefinitions
            .FirstOrDefaultAsync(entity => entity.Key.ToLower() == key.ToLower(), ct);

        if (existing is null)
        {
            definition.Key = key;
            definition.UpdatedAt = DateTime.UtcNow;
            context.MetadataDefinitions.Add(definition);
            await context.SaveChangesAsync(ct);
            return definition;
        }

        existing.Label = definition.Label;
        existing.ValueType = definition.ValueType;
        existing.Filterable = definition.Filterable;
        existing.AllowFreeText = definition.AllowFreeText;
        existing.Required = definition.Required;
        existing.Policy = definition.Policy;
        existing.IsActive = definition.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);
        return existing;
    }

    public async Task<MetadataValueOption> UpsertValueOptionAsync(string key, MetadataValueOption option, CancellationToken ct)
    {
        var definition = await GetDefinitionByKeyAsync(key, ct)
            ?? throw new KeyNotFoundException($"Metadata definition '{key}' was not found.");

        var canonicalValue = option.CanonicalValue.Trim();
        var existing = await context.MetadataValueOptions
            .FirstOrDefaultAsync(entity => entity.MetadataDefinitionId == definition.Id
                && entity.CanonicalValue.ToLower() == canonicalValue.ToLower(), ct);

        if (existing is null)
        {
            option.MetadataDefinitionId = definition.Id;
            option.CanonicalValue = canonicalValue;
            option.UpdatedAt = DateTime.UtcNow;
            context.MetadataValueOptions.Add(option);
            await context.SaveChangesAsync(ct);
            return option;
        }

        existing.DisplayLabel = option.DisplayLabel;
        existing.Aliases = option.Aliases;
        existing.IsActive = option.IsActive;
        existing.SortOrder = option.SortOrder;
        existing.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);
        return existing;
    }
}
