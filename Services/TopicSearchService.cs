using Microsoft.EntityFrameworkCore;
using TaskManager.Data;

namespace TaskManager.Services;

public class TopicSearchService(AppDbContext db)
{
    private const int MinQueryLength = 2;

    public async Task<TopicSearchResult> SearchAsync(
        int userId,
        string query,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        query = query.Trim();

        if (query.Length < MinQueryLength)
            return new TopicSearchResult(query, page, pageSize, 0, []);

        var pattern = $"%{EscapeLike(query)}%";

        var filtered = db.Topics
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .Where(t =>
                (t.Type == "topic" && EF.Functions.ILike(t.Name, pattern))
                || (t.Type == "task" && EF.Functions.ILike(t.Name, pattern))
                || (t.Type == "task" && db.TaskItems.Any(ti =>
                    ti.TopicId == t.Id && EF.Functions.ILike(ti.Description, pattern))));

        var totalCount = await filtered.CountAsync(cancellationToken);

        var pageItems = await filtered
            .OrderBy(t => t.Type == "task" ? 1 : 0)
            .ThenBy(t => t.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        if (pageItems.Count == 0)
            return new TopicSearchResult(query, page, pageSize, totalCount, []);

        var taskIds = pageItems
            .Where(t => t.Type == "task")
            .Select(t => t.Id)
            .ToList();

        var tasks = taskIds.Count == 0
            ? []
            : await db.TaskItems
                .AsNoTracking()
                .Where(ti => taskIds.Contains(ti.TopicId))
                .ToDictionaryAsync(ti => ti.TopicId, cancellationToken);

        var allNodes = await db.Topics
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .Select(t => new TopicPathNode(t.Id, t.Name, t.ParentId))
            .ToListAsync(cancellationToken);

        var byId = allNodes.ToDictionary(n => n.Id);

        var items = pageItems.Select(t =>
        {
            tasks.TryGetValue(t.Id, out var task);

            return new TopicSearchItem(
                t.Id,
                t.Name,
                t.Type,
                t.ParentId,
                BuildPath(t.ParentId, byId),
                task?.Description,
                task?.Status.ToString());
        }).ToList();

        return new TopicSearchResult(query, page, pageSize, totalCount, items);
    }

    private static List<TopicPathSegment> BuildPath(int? parentId, Dictionary<int, TopicPathNode> byId)
    {
        var path = new List<TopicPathSegment>();
        var current = parentId;

        while (current is int id && byId.TryGetValue(id, out var node))
        {
            path.Add(new TopicPathSegment(node.Id, node.Name));
            current = node.ParentId;
        }

        path.Reverse();
        return path;
    }

    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    private sealed record TopicPathNode(int Id, string Name, int? ParentId);
}

public sealed record TopicSearchItem(
    int Id,
    string Name,
    string Type,
    int? ParentId,
    IReadOnlyList<TopicPathSegment> Path,
    string? Description,
    string? Status);

public sealed record TopicPathSegment(int Id, string Name);

public sealed record TopicSearchResult(
    string Query,
    int Page,
    int PageSize,
    int TotalCount,
    IReadOnlyList<TopicSearchItem> Items)
{
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;

    public bool HasMore => Page < TotalPages;
}
