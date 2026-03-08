using System.Text.Json;
using Aimy.Core.Application.DTOs.Metadata;
using Aimy.Core.Application.Interfaces.Ingestion;
using Aimy.Core.Application.Interfaces.KnowledgeBase;
using Aimy.Core.Application.Interfaces.Metadata;
using Aimy.Core.Application.Interfaces.Upload;
using Aimy.Core.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Aimy.Core.Application.Services;

public class UploadKnowledgeSyncService(
    IUploadRepository uploadRepository,
    IKnowledgeItemRepository knowledgeItemRepository,
    IDataIngestionService dataIngestionService,
    IIngestionJobService ingestionJobService,
    IMetadataCatalogRepository metadataCatalogRepository,
    ILogger<UploadKnowledgeSyncService> logger) : IUploadKnowledgeSyncService
{
    private const double FuzzySimilarityThreshold = 0.82;
    private const int MaxFuzzyDistance = 2;

    public string? NormalizeMetadataPayload(string? metadata)
    {
        var result = NormalizeMetadataAsync(metadata, MetadataNormalizationPolicy.Permissive, CancellationToken.None)
            .GetAwaiter()
            .GetResult();

        return result.NormalizedMetadata;
    }

    public async Task<MetadataNormalizationResult> NormalizeMetadataAsync(string? metadata, MetadataNormalizationPolicy defaultPolicy, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(metadata))
        {
            return new MetadataNormalizationResult
            {
                NormalizedMetadata = null,
                HasChanges = false,
                Warnings = []
            };
        }

        JsonElement root;
        try
        {
            using var document = JsonDocument.Parse(metadata);
            root = document.RootElement.Clone();
        }
        catch (JsonException)
        {
            return new MetadataNormalizationResult
            {
                NormalizedMetadata = null,
                HasChanges = true,
                Warnings =
                [
                    new MetadataNormalizationWarning
                    {
                        Key = "metadata",
                        Message = "Invalid metadata JSON payload.",
                        MatchType = MetadataMatchType.Rejected
                    }
                ]
            };
        }

        if (root.ValueKind != JsonValueKind.Object)
        {
            return new MetadataNormalizationResult
            {
                NormalizedMetadata = null,
                HasChanges = true,
                Warnings =
                [
                    new MetadataNormalizationWarning
                    {
                        Key = "metadata",
                        Message = "Metadata payload must be a JSON object.",
                        MatchType = MetadataMatchType.Rejected
                    }
                ]
            };
        }

        var definitions = await metadataCatalogRepository.GetDefinitionsAsync(ct);
        var definitionsByKey = definitions.ToDictionary(d => d.Key, StringComparer.OrdinalIgnoreCase);
        var normalized = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var warnings = new List<MetadataNormalizationWarning>();

        foreach (var property in root.EnumerateObject())
        {
            var key = property.Name.Trim();
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            if (!definitionsByKey.TryGetValue(key, out var definition))
            {
                if (defaultPolicy == MetadataNormalizationPolicy.Strict)
                {
                    warnings.Add(new MetadataNormalizationWarning
                    {
                        Key = key,
                        InputValue = property.Value.GetRawText(),
                        Message = "Unknown key rejected by strict policy.",
                        MatchType = MetadataMatchType.Rejected
                    });
                    continue;
                }

                normalized[key] = ExtractObjectValue(property.Value);
                warnings.Add(new MetadataNormalizationWarning
                {
                    Key = key,
                    InputValue = property.Value.GetRawText(),
                    ResolvedValue = property.Value.GetRawText(),
                    Message = "Unknown key accepted as custom metadata.",
                    MatchType = MetadataMatchType.Custom
                });
                continue;
            }

            var outputKey = definition.Key;
            var value = property.Value;
            if (value.ValueKind != JsonValueKind.String)
            {
                normalized[outputKey] = ExtractObjectValue(value);
                continue;
            }

            var inputText = value.GetString();
            if (string.IsNullOrWhiteSpace(inputText))
            {
                normalized[outputKey] = string.Empty;
                continue;
            }

            var options = await metadataCatalogRepository.GetAllValueOptionsAsync(definition.Key, ct);
            var resolution = ResolveValue(inputText!, options);

            if (resolution.MatchType is MetadataMatchType.ExactCanonical or MetadataMatchType.Alias or MetadataMatchType.Fuzzy)
            {
                normalized[outputKey] = resolution.CanonicalValue!;
                if (!string.Equals(inputText, resolution.CanonicalValue, StringComparison.Ordinal))
                {
                    warnings.Add(new MetadataNormalizationWarning
                    {
                        Key = outputKey,
                        InputValue = inputText,
                        ResolvedValue = resolution.CanonicalValue,
                        Message = $"Input resolved using {resolution.MatchType} match.",
                        MatchType = resolution.MatchType
                    });
                }

                continue;
            }

            if (definition.AllowFreeText || definition.Policy == MetadataNormalizationPolicy.Permissive)
            {
                normalized[outputKey] = inputText;
                warnings.Add(new MetadataNormalizationWarning
                {
                    Key = outputKey,
                    InputValue = inputText,
                    ResolvedValue = inputText,
                    Message = "Value accepted as custom input.",
                    MatchType = MetadataMatchType.Custom
                });
                continue;
            }

            warnings.Add(new MetadataNormalizationWarning
            {
                Key = outputKey,
                InputValue = inputText,
                Message = "Value rejected by strict definition policy.",
                MatchType = MetadataMatchType.Rejected
            });
        }

        var normalizedJson = normalized.Count == 0 ? null : JsonSerializer.Serialize(normalized);
        var hasChanges = !JsonEquals(metadata, normalizedJson);

        var unresolvedCount = warnings.Count(w => w.MatchType is MetadataMatchType.Rejected or MetadataMatchType.Custom);
        var autoResolvedCount = warnings.Count(w => w.MatchType is MetadataMatchType.Alias or MetadataMatchType.Fuzzy);
        logger.LogInformation(
            "Metadata normalization completed. AutoResolved={AutoResolvedCount}, Unresolved={UnresolvedCount}",
            autoResolvedCount,
            unresolvedCount);

        return new MetadataNormalizationResult
        {
            NormalizedMetadata = normalizedJson,
            HasChanges = hasChanges,
            Warnings = warnings
        };
    }

    public Task<IReadOnlyList<MetadataDefinition>> GetMetadataDefinitionsAsync(CancellationToken ct)
    {
        return metadataCatalogRepository.GetDefinitionsAsync(ct);
    }

    public async Task<MetadataValueSuggestions> GetMetadataValueSuggestionsAsync(string key, string? prefix, CancellationToken ct)
    {
        var options = await metadataCatalogRepository.GetValueOptionsAsync(key, prefix, ct);
        var items = options
            .Select(option => new MetadataValueSuggestionItem
            {
                Value = option.CanonicalValue,
                Label = option.DisplayLabel,
                Aliases = option.Aliases,
                MatchType = string.IsNullOrWhiteSpace(prefix)
                    ? "catalog"
                    : GetSuggestionMatchType(prefix!, option)
            })
            .ToList();

        return new MetadataValueSuggestions
        {
            Key = key,
            Items = items
        };
    }

    public Task<MetadataDefinition> UpsertDefinitionAsync(MetadataDefinition definition, CancellationToken ct)
    {
        return metadataCatalogRepository.UpsertDefinitionAsync(definition, ct);
    }

    public async Task<MetadataValueOption> UpsertValueOptionAsync(string key, MetadataValueOption option, CancellationToken ct)
    {
        var keyOptions = await metadataCatalogRepository.GetAllValueOptionsAsync(key, ct);
        var normalizedAliases = option.Aliases
            .Where(alias => !string.IsNullOrWhiteSpace(alias))
            .Select(alias => alias.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var aliasOwner = keyOptions.FirstOrDefault(existing =>
            !string.Equals(existing.CanonicalValue, option.CanonicalValue, StringComparison.OrdinalIgnoreCase)
            && existing.Aliases.Any(alias => normalizedAliases.Contains(alias, StringComparer.OrdinalIgnoreCase)));

        if (aliasOwner is not null)
        {
            throw new InvalidOperationException($"Alias conflict detected for key '{key}'.");
        }

        option.Aliases = normalizedAliases;
        return await metadataCatalogRepository.UpsertValueOptionAsync(key, option, ct);
    }

    public async Task SyncMetadataAsync(Aimy.Core.Domain.Entities.Upload upload, string? metadata, CancellationToken ct)
    {
        var normalization = await NormalizeMetadataAsync(metadata, MetadataNormalizationPolicy.Permissive, ct);
        var canonicalMetadata = normalization.NormalizedMetadata;

        upload.Metadata = canonicalMetadata;
        await uploadRepository.UpdateAsync(upload, ct);
        await dataIngestionService.UpdateMetadataByUploadIdAsync(upload.Id, canonicalMetadata, ct);

        var linkedItems = await knowledgeItemRepository.GetBySourceUploadIdAsync(upload.Id, ct);
        foreach (var linkedItem in linkedItems)
        {
            linkedItem.Metadata = canonicalMetadata;
            linkedItem.UpdatedAt = DateTime.UtcNow;
            await knowledgeItemRepository.UpdateAsync(linkedItem, ct);
        }
    }

    public Task EnqueueIngestionAsync(Guid uploadId, CancellationToken ct)
    {
        return ingestionJobService.EnqueueAsync(uploadId, ct);
    }

    public async Task ReingestAsync(Guid uploadId, CancellationToken ct)
    {
        await dataIngestionService.DeleteByUploadIdAsync(uploadId, ct);
        await ingestionJobService.EnqueueAsync(uploadId, ct);
    }

    private static object? ExtractObjectValue(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.False => false,
            JsonValueKind.True => true,
            JsonValueKind.Number => value.TryGetInt64(out var number) ? number : value.GetDouble(),
            JsonValueKind.String => value.GetString(),
            _ => JsonSerializer.Deserialize<object>(value.GetRawText())
        };
    }

    private static bool JsonEquals(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left) && string.IsNullOrWhiteSpace(right))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
        {
            return false;
        }

        try
        {
            using var leftDoc = JsonDocument.Parse(left);
            using var rightDoc = JsonDocument.Parse(right);
            return leftDoc.RootElement.ToString() == rightDoc.RootElement.ToString();
        }
        catch (JsonException)
        {
            return string.Equals(left, right, StringComparison.Ordinal);
        }
    }

    private static string GetSuggestionMatchType(string prefix, MetadataValueOption option)
    {
        if (option.CanonicalValue.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return "prefix";
        }

        if (option.Aliases.Any(alias => alias.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
        {
            return "alias";
        }

        if (option.DisplayLabel.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return "label";
        }

        return "catalog";
    }

    private static (MetadataMatchType MatchType, string? CanonicalValue) ResolveValue(
        string input,
        IReadOnlyList<MetadataValueOption> options)
    {
        if (options.Count == 0)
        {
            return (MetadataMatchType.Custom, null);
        }

        var exact = options.FirstOrDefault(option => string.Equals(option.CanonicalValue, input, StringComparison.OrdinalIgnoreCase));
        if (exact is not null)
        {
            return (MetadataMatchType.ExactCanonical, exact.CanonicalValue);
        }

        var alias = options.FirstOrDefault(option => option.Aliases.Any(existingAlias =>
            string.Equals(existingAlias, input, StringComparison.OrdinalIgnoreCase)));
        if (alias is not null)
        {
            return (MetadataMatchType.Alias, alias.CanonicalValue);
        }

        var fuzzyCandidates = options
            .Select(option => new
            {
                option.CanonicalValue,
                Distance = LevenshteinDistance(option.CanonicalValue, input)
            })
            .Where(candidate => candidate.Distance <= MaxFuzzyDistance)
            .Select(candidate => new
            {
                candidate.CanonicalValue,
                candidate.Distance,
                Similarity = CalculateSimilarity(candidate.CanonicalValue, input)
            })
            .Where(candidate => candidate.Similarity >= FuzzySimilarityThreshold)
            .OrderBy(candidate => candidate.Distance)
            .ThenByDescending(candidate => candidate.Similarity)
            .ToList();

        if (fuzzyCandidates.Count == 1)
        {
            return (MetadataMatchType.Fuzzy, fuzzyCandidates[0].CanonicalValue);
        }

        if (fuzzyCandidates.Count > 1)
        {
            var bestDistance = fuzzyCandidates[0].Distance;
            var topCandidates = fuzzyCandidates.Where(candidate => candidate.Distance == bestDistance)
                .Select(candidate => candidate.CanonicalValue)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (topCandidates.Count == 1)
            {
                return (MetadataMatchType.Fuzzy, topCandidates[0]);
            }
        }

        return (MetadataMatchType.Custom, null);
    }

    private static int LevenshteinDistance(string left, string right)
    {
        var source = left.ToLowerInvariant();
        var target = right.ToLowerInvariant();
        var matrix = new int[source.Length + 1, target.Length + 1];

        for (var i = 0; i <= source.Length; i++)
        {
            matrix[i, 0] = i;
        }

        for (var j = 0; j <= target.Length; j++)
        {
            matrix[0, j] = j;
        }

        for (var i = 1; i <= source.Length; i++)
        {
            for (var j = 1; j <= target.Length; j++)
            {
                var cost = source[i - 1] == target[j - 1] ? 0 : 1;
                matrix[i, j] = Math.Min(
                    Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost);
            }
        }

        return matrix[source.Length, target.Length];
    }

    private static double CalculateSimilarity(string left, string right)
    {
        var maxLength = Math.Max(left.Length, right.Length);
        if (maxLength == 0)
        {
            return 1;
        }

        var distance = LevenshteinDistance(left, right);
        return 1d - (double)distance / maxLength;
    }
}
