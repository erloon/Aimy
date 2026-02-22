using Aimy.Core.Application.Interfaces.Auth;
using Aimy.Core.Application.Interfaces.Ingestion;
using Aimy.Core.Application.Interfaces.KnowledgeBase;
using Aimy.Core.Application.Interfaces.Upload;

namespace Aimy.Core.Application.Services;

using Aimy.Core.Application.DTOs;
using Aimy.Core.Application.DTOs.KnowledgeBase;
using Aimy.Core.Domain.Entities;

public class KnowledgeItemService(
    IKnowledgeBaseRepository kbRepository,
    IFolderRepository folderRepository,
    IKnowledgeItemRepository itemRepository,
    IUploadRepository uploadRepository,
    IDataIngestionService dataIngestionService,
    IStorageService storageService,
    ICurrentUserService currentUserService) : IKnowledgeItemService
{
    public async Task<ItemResponse> CreateNoteAsync(CreateNoteRequest request, CancellationToken ct)
    {
        var userId = currentUserService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("User is not authenticated");

        // Validate folder ownership
        var folder = await folderRepository.GetByIdAsync(request.FolderId, ct)
            ?? throw new KeyNotFoundException("Folder not found");

        var kb = await kbRepository.GetOrCreateForUserAsync(userId, ct);
        if (folder.KnowledgeBaseId != kb.Id)
            throw new UnauthorizedAccessException("Folder does not belong to user");

        // Create upload for markdown content
        var contentBytes = System.Text.Encoding.UTF8.GetBytes(request.Content ?? "");
        using var stream = new MemoryStream(contentBytes);
        stream.Position = 0;

        var storagePath = await storageService.UploadAsync(
            userId,
            $"{request.Title}.md",
            stream,
            "text/markdown",
            ct);

        var upload = new Upload
        {
            UserId = userId,
            FileName = $"{request.Title}.md",
            StoragePath = storagePath,
            FileSizeBytes = contentBytes.Length,
            ContentType = "text/markdown",
            Metadata = request.Metadata
        };

        var savedUpload = await uploadRepository.AddAsync(upload, ct);

        // Create knowledge item linked to upload
        // Note: If this fails, we keep the upload (no rollback) as per plan
        var item = new KnowledgeItem
        {
            FolderId = request.FolderId,
            Title = request.Title,
            ItemType = KnowledgeItemType.Note,
            Content = request.Content,
            Metadata = request.Metadata,
            SourceUploadId = savedUpload.Id
        };

        var savedItem = await itemRepository.AddAsync(item, ct);
        return MapToResponse(savedItem);
    }

    public async Task<ItemResponse> CreateFromUploadAsync(CreateItemFromUploadRequest request, CancellationToken ct)
    {
        var userId = currentUserService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("User is not authenticated");

        // Validate folder ownership
        var folder = await folderRepository.GetByIdAsync(request.FolderId, ct)
            ?? throw new KeyNotFoundException("Folder not found");

        var kb = await kbRepository.GetOrCreateForUserAsync(userId, ct);
        if (folder.KnowledgeBaseId != kb.Id)
            throw new UnauthorizedAccessException("Folder does not belong to user");

        // Validate upload ownership
        var upload = await uploadRepository.GetByIdAsync(request.UploadId, ct)
            ?? throw new KeyNotFoundException("Upload not found");

        if (upload.UserId != userId)
            throw new UnauthorizedAccessException("Upload does not belong to user");

        var resolvedMetadata = string.IsNullOrWhiteSpace(request.Metadata)
            ? upload.Metadata
            : request.Metadata;

        if (!string.IsNullOrWhiteSpace(request.Metadata))
        {
            upload.Metadata = request.Metadata;
            await uploadRepository.UpdateAsync(upload, ct);
            await dataIngestionService.UpdateMetadataByUploadIdAsync(upload.Id, upload.Metadata, ct);
        }

        var item = new KnowledgeItem
        {
            FolderId = request.FolderId,
            Title = request.Title ?? upload.FileName,
            ItemType = KnowledgeItemType.File,
            Metadata = resolvedMetadata,
            SourceUploadId = upload.Id
        };

        var savedItem = await itemRepository.AddAsync(item, ct);
        return MapToResponse(savedItem);
    }

    public async Task<ItemResponse> UpdateAsync(Guid id, UpdateItemRequest request, CancellationToken ct)
    {
        var userId = currentUserService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("User is not authenticated");

        var item = await itemRepository.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException("Item not found");

        // Validate ownership through folder -> KB chain
        if (item.Folder?.KnowledgeBase?.UserId != userId)
            throw new UnauthorizedAccessException("Item does not belong to user");

        var originalTitle = item.Title;
        var originalContent = item.Content;
        var titleChanged = request.Title is not null && !string.Equals(request.Title, originalTitle, StringComparison.Ordinal);
        var contentChanged = request.Content is not null && !string.Equals(request.Content, originalContent, StringComparison.Ordinal);
        var metadataChanged = request.Metadata is not null;

        Upload? sourceUpload = null;
        if (item.SourceUploadId.HasValue && (metadataChanged || (item.ItemType == KnowledgeItemType.Note && (titleChanged || contentChanged))))
        {
            sourceUpload = await uploadRepository.GetByIdAsync(item.SourceUploadId.Value, ct)
                ?? throw new KeyNotFoundException("Upload not found");

            if (sourceUpload.UserId != userId)
                throw new UnauthorizedAccessException("Upload does not belong to user");
        }

        if (item.ItemType == KnowledgeItemType.Note && sourceUpload is not null && (titleChanged || contentChanged))
        {
            var updatedTitle = request.Title ?? originalTitle;
            var updatedContent = request.Content ?? originalContent;
            var contentBytes = System.Text.Encoding.UTF8.GetBytes(updatedContent ?? string.Empty);
            using var stream = new MemoryStream(contentBytes);
            stream.Position = 0;

            var newStoragePath = await storageService.UploadAsync(
                userId,
                $"{updatedTitle}.md",
                stream,
                "text/markdown",
                ct);

            await storageService.DeleteAsync(sourceUpload.StoragePath, ct);

            sourceUpload.FileName = $"{updatedTitle}.md";
            sourceUpload.StoragePath = newStoragePath;
            sourceUpload.FileSizeBytes = contentBytes.Length;
            sourceUpload.ContentType = "text/markdown";
        }

        if (sourceUpload is not null && metadataChanged)
        {
            sourceUpload.Metadata = request.Metadata;
        }

        if (sourceUpload is not null)
        {
            await uploadRepository.UpdateAsync(sourceUpload, ct);

            if (metadataChanged)
            {
                await dataIngestionService.UpdateMetadataByUploadIdAsync(sourceUpload.Id, sourceUpload.Metadata, ct);
            }
        }

        if (request.Title is not null)
            item.Title = request.Title;
        if (request.Content is not null)
            item.Content = request.Content;
        if (request.Metadata is not null)
            item.Metadata = request.Metadata;
        if (request.FolderId.HasValue)
        {
            // Validate new folder ownership
            var newFolder = await folderRepository.GetByIdAsync(request.FolderId.Value, ct)
                ?? throw new KeyNotFoundException("Target folder not found");

            if (newFolder.KnowledgeBase?.UserId != userId)
                throw new UnauthorizedAccessException("Target folder does not belong to user");

            item.FolderId = request.FolderId.Value;
        }

        item.UpdatedAt = DateTime.UtcNow;

        await itemRepository.UpdateAsync(item, ct);
        return MapToResponse(item);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var userId = currentUserService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("User is not authenticated");

        var item = await itemRepository.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException("Item not found");

        // Validate ownership
        if (item.Folder?.KnowledgeBase?.UserId != userId)
            throw new UnauthorizedAccessException("Item does not belong to user");

        // IMPORTANT: For notes, we UNLINK only - do NOT delete the underlying upload
        // This is by design per the plan
        await itemRepository.DeleteAsync(id, ct);
    }

    public async Task<ItemResponse?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var userId = currentUserService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("User is not authenticated");

        var item = await itemRepository.GetByIdAsync(id, ct);
        if (item == null)
            return null;

        // Validate ownership
        if (item.Folder?.KnowledgeBase?.UserId != userId)
            throw new UnauthorizedAccessException("Item does not belong to user");

        return MapToResponse(item);
    }

    public async Task<PagedResult<ItemResponse>> SearchAsync(ItemSearchRequest request, CancellationToken ct)
    {
        var userId = currentUserService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("User is not authenticated");

        var kb = await kbRepository.GetOrCreateForUserAsync(userId, ct);

        IReadOnlyCollection<Guid>? folderIds = null;
        if (request.FolderId.HasValue && request.IncludeSubFolders)
        {
            var folderTree = await folderRepository.GetFolderTreeAsync(kb.Id, ct);
            var descendantsByParent = folderTree
                .Where(f => f.ParentFolderId.HasValue)
                .GroupBy(f => f.ParentFolderId!.Value)
                .ToDictionary(g => g.Key, g => g.Select(f => f.Id).ToList());

            var resolvedFolderIds = new List<Guid> { request.FolderId.Value };
            var queue = new Queue<Guid>();
            queue.Enqueue(request.FolderId.Value);

            while (queue.Count > 0)
            {
                var currentFolderId = queue.Dequeue();
                if (!descendantsByParent.TryGetValue(currentFolderId, out var childIds))
                {
                    continue;
                }

                foreach (var childId in childIds)
                {
                    resolvedFolderIds.Add(childId);
                    queue.Enqueue(childId);
                }
            }

            folderIds = resolvedFolderIds;
        }

        var result = await itemRepository.SearchAsync(
            kb.Id,
            request.FolderId,
            folderIds,
            request.Search,
            request.Metadata,
            request.Type,
            request.Page,
            request.PageSize,
            ct);

        return new PagedResult<ItemResponse>
        {
            Items = result.Items.Select(MapToResponse).ToList(),
            Page = result.Page,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount
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
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
    }
}
