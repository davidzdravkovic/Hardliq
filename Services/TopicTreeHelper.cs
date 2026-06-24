using Microsoft.EntityFrameworkCore;
using TaskManager.Data;
using TaskManager.Models;

namespace TaskManager.Services;

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

        var folderCount = 0;
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
                folderCount++;
            else
                taskCount++;

            if (childrenByParent.TryGetValue(current.Id, out var nested))
            {
                foreach (var child in nested)
                    queue.Enqueue(child);
            }
        }

        return (folderCount, taskCount);
    }

    public static async Task<HashSet<int>> CollectDescendantTaskTopicIdsAsync(
        AppDbContext db,
        int userId,
        int topicId,
        CancellationToken cancellationToken = default)
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

    private sealed record TopicNode(int Id, int? ParentId, string Type);
}
