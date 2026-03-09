namespace Aimy.Infrastructure.Storage;

public static class StorageBuckets
{
    public const string KnowledgeBase = "knowledgebase";
}

public static class BucketNames
{
    public const string KnowledgeBase = StorageBuckets.KnowledgeBase;
}

public static class StorageKeyFormat
{
    public static string KbItemKey(Guid userId, string fileName) => $"{userId}/items/{Guid.NewGuid()}_{fileName}";

    // [DECISION NEEDED: legacy MinIO objects strategy]
    // Existing objects currently stored in per-user buckets ({userId} as bucket name)
    // need a migration/backward-compatibility policy before central-bucket rollout is finalized.
}
