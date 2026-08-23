using Microsoft.EntityFrameworkCore;
using TaskManager.Data;
using TaskManager.Models;

namespace TaskManager.Utilities;

public static class TopicTreeHelper
{
    public static async Task<(int FolderCount, int TaskCount)> CountDescendantsAsync(
        AppDbContext db,
        int userId,
        int topicId)
    {
        var nodes = await db.Topics
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .Select(t => new TopicNode(t.Id, t.ParentId, t.Type))
            .ToListAsync();

        if (nodes.All(t => t.Id != topicId))
            return (0, 0);

        var childrenByParent = nodes
            .Where(t => t.ParentId is not null)
            .GroupBy(t => t.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var topicsCount = 0;
        var taskCount = 0;
        var queue = new Queue<TopicNode>();

        if (childrenByParent.TryGetValue(topicId, out var directChildren))
        {
            foreach (var child in directChildren)
                queue.Enqueue(child);
        }

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (current.Type == "topic")
                topicsCount++;
            else
                taskCount++;

            if (childrenByParent.TryGetValue(current.Id, out var nested))
            {
                foreach (var child in nested)
                    queue.Enqueue(child);
            }
        }

        return (topicsCount, taskCount);
    }

    public static async Task<List<Topic>> CollectDescendantTaskTopicIdsAsync(
        AppDbContext db,
        int userId,
        int topicId,
        CancellationToken cancellationToken = default)
    {
        var nodes = await db.Topics
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .ToListAsync(cancellationToken);

        if (nodes.All(t => t.Id != topicId))
            return [];

        var childrenByParent = nodes
            .Where(t => t.ParentId is not null)
            .GroupBy(t => t.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var tasks = new List<Topic>();
        var queue = new Queue<Topic>();

        if (childrenByParent.TryGetValue(topicId, out var directChildren))
        {
            foreach (var child in directChildren)
                queue.Enqueue(child);
        }

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (current.Type == "task")
                tasks.Add(current);

            if (childrenByParent.TryGetValue(current.Id, out var nested))
            {
                foreach (var child in nested)
                    queue.Enqueue(child);
            }
        }

        return tasks;
    }

    public static async Task<bool> IsDescendantAsync(
        AppDbContext db,
        int userId,
        int ancestorId,
        int candidateId,
        CancellationToken cancellationToken = default)
    {
        if (ancestorId == candidateId)
            return true;

        var nodes = await db.Topics
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .Select(t => new TopicNode(t.Id, t.ParentId, t.Type))
            .ToListAsync(cancellationToken);

        var childrenByParent = nodes
            .Where(t => t.ParentId is not null)
            .GroupBy(t => t.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Id).ToList());

        var queue = new Queue<int>();
        queue.Enqueue(ancestorId);

        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();
            if (!childrenByParent.TryGetValue(currentId, out var childIds))
                continue;

            foreach (var childId in childIds)
            {
                if (childId == candidateId)
                    return true;

                queue.Enqueue(childId);
            }
        }

        return false;
    }

    public static async Task<int> GetDescendantFolderDepthAsync(
        AppDbContext db,
        int userId,
        int topicId,
        CancellationToken cancellationToken = default)
    {
        var nodes = await db.Topics
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.Type == "topic")
            .Select(t => new { t.Id, t.ParentId })
            .ToListAsync(cancellationToken);

        var childrenByParent = nodes
            .Where(t => t.ParentId is not null)
            .GroupBy(t => t.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Id).ToList());

        var maxExtra = 0;
        var queue = new Queue<(int Id, int Depth)>();

        if (childrenByParent.TryGetValue(topicId, out var directChildren))
        {
            foreach (var childId in directChildren)
                queue.Enqueue((childId, 1));
        }

        while (queue.Count > 0)
        {
            var (id, depth) = queue.Dequeue();
            if (depth > maxExtra)
                maxExtra = depth;

            if (childrenByParent.TryGetValue(id, out var nested))
            {
                foreach (var childId in nested)
                    queue.Enqueue((childId, depth + 1));
            }
        }

        return maxExtra;
    }

    private sealed record TopicNode(int Id, int? ParentId, string Type);
}
