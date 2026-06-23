using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TaskManager.Data;
using TaskManager.Models;

namespace TaskManager.Services;

public sealed class TaskStatsService(AppDbContext db, IMemoryCache cache)
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(10);

    public async Task<TaskStatsResult> GetStatsAsync(
        int userId,
        int? topicId,
        DateTime? since,
        CancellationToken cancellationToken = default)
    {
        var executedSince = since ?? GetStartOfUtcWeek(DateTime.UtcNow);
        var cacheKey = $"task-stats:{userId}:{topicId?.ToString() ?? "all"}:{executedSince:yyyyMMdd}";

        if (cache.TryGetValue(cacheKey, out TaskStatsResult? cached) && cached is not null)
            return cached;

        TaskStatsResult result;

        if (topicId is null)
        {
            result = await BuildWorkspaceStatsAsync(userId, executedSince, cancellationToken);
        }
        else
        {
            var topic = await db.Topics
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    t => t.Id == topicId && t.UserId == userId && t.Type == "topic",
                    cancellationToken);

            if (topic is null)
                throw new KeyNotFoundException("Topic not found.");

            result = await BuildFolderStatsAsync(userId, topic, executedSince, cancellationToken);
        }

        cache.Set(cacheKey, result, CacheTtl);
        return result;
    }

    public void Invalidate(int userId)
    {
        // Short TTL handles freshness; this is a hook for future explicit invalidation.
        cache.Remove($"task-stats:{userId}:all:{GetStartOfUtcWeek(DateTime.UtcNow):yyyyMMdd}");
    }

    private async Task<TaskStatsResult> BuildWorkspaceStatsAsync(
        int userId,
        DateTime executedSince,
        CancellationToken cancellationToken)
    {
        var tasks = db.TaskItems
            .AsNoTracking()
            .Where(t => t.Topic.UserId == userId);

        return await BuildResultAsync(
            scope: "workspace",
            topicId: null,
            topicName: null,
            tasks,
            executedSince,
            cancellationToken);
    }

    private async Task<TaskStatsResult> BuildFolderStatsAsync(
        int userId,
        Topic topic,
        DateTime executedSince,
        CancellationToken cancellationToken)
    {
        var taskTopicIds = await CollectDescendantTaskTopicIdsAsync(userId, topic.Id, cancellationToken);

        var tasks = db.TaskItems
            .AsNoTracking()
            .Where(t => taskTopicIds.Contains(t.TopicId));

        return await BuildResultAsync(
            scope: "folder",
            topicId: topic.Id,
            topicName: topic.Name,
            tasks,
            executedSince,
            cancellationToken);
    }

    private static async Task<TaskStatsResult> BuildResultAsync(
        string scope,
        int? topicId,
        string? topicName,
        IQueryable<TaskItem> tasks,
        DateTime executedSince,
        CancellationToken cancellationToken)
    {
        var statusCounts = await tasks
            .GroupBy(t => t.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var pending = statusCounts.FirstOrDefault(x => x.Status == TaskItemStatus.Pending)?.Count ?? 0;
        var completed = statusCounts.FirstOrDefault(x => x.Status == TaskItemStatus.Completed)?.Count ?? 0;
        var canceled = statusCounts.FirstOrDefault(x => x.Status == TaskItemStatus.Canceled)?.Count ?? 0;

        var completedSince = await tasks.CountAsync(
            t => t.Status == TaskItemStatus.Completed &&
                 t.CompletedAt != null &&
                 t.CompletedAt >= executedSince,
            cancellationToken);

        var canceledSince = await tasks.CountAsync(
            t => t.Status == TaskItemStatus.Canceled &&
                 t.CanceledAt != null &&
                 t.CanceledAt >= executedSince,
            cancellationToken);

        return new TaskStatsResult(
            scope,
            topicId,
            topicName,
            pending + completed + canceled,
            pending,
            completed,
            canceled,
            executedSince,
            completedSince,
            canceledSince);
    }

    private async Task<HashSet<int>> CollectDescendantTaskTopicIdsAsync(
        int userId,
        int topicId,
        CancellationToken cancellationToken)
    {
        var nodes = await db.Topics
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .Select(t => new TopicNode(t.Id, t.ParentId, t.Type))
            .ToListAsync(cancellationToken);

        if (nodes.All(t => t.Id != topicId))
            return [];

        var childrenByParent = nodes
            .Where(t => t.ParentId is not null)
            .GroupBy(t => t.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var taskTopicIds = new HashSet<int>();
        var queue = new Queue<TopicNode>();

        if (childrenByParent.TryGetValue(topicId, out var directChildren))
        {
            foreach (var child in directChildren)
                queue.Enqueue(child);
        }

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (current.Type == "task")
                taskTopicIds.Add(current.Id);

            if (childrenByParent.TryGetValue(current.Id, out var nested))
            {
                foreach (var child in nested)
                    queue.Enqueue(child);
            }
        }

        return taskTopicIds;
    }

    private static DateTime GetStartOfUtcWeek(DateTime utcNow)
    {
        var dayOfWeek = (int)utcNow.DayOfWeek;
        var daysFromMonday = dayOfWeek == 0 ? 6 : dayOfWeek - 1;
        return utcNow.Date.AddDays(-daysFromMonday);
    }

    private sealed record TopicNode(int Id, int? ParentId, string Type);
}

public sealed record TaskStatsResult(
    string Scope,
    int? TopicId,
    string? TopicName,
    int TotalTasks,
    int Pending,
    int Completed,
    int Canceled,
    DateTime ExecutedSince,
    int CompletedSince,
    int CanceledSince);
