using Microsoft.EntityFrameworkCore;
using TaskManager.Data;
using TaskManager.Domain;
using TaskManager.Dto.HelpersDto;
using TaskManager.Dto.RequestsDto;
using TaskManager.Dto.ResponsesDto;
using TaskManager.Errors;
using TaskManager.Models;
using TaskManager.Utilities;

namespace TaskManager.Services;

public class TopicService(AppDbContext db, TaskStatsService taskStatsService, TopicSearchService searchService)
{
    public async Task<Result<TopicResponse>> CreateTopicAsync(int userId, CreateTopicRequest request)
    {
        if (request.ParentId is int parentId)
        {
            var parent = await db.Topics
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == parentId && t.UserId == userId);

            if (parent is null)
                return Result<TopicResponse>.Fail(ErrorCodes.TopicNotFound);

            if (parent.Type != "topic")
                return Result<TopicResponse>.Fail(ErrorCodes.ParentTypeTaskNotAllowed);

            var parentDepth = await GetDepthAsync(userId, parent.Id);
            if (parentDepth >= TreePolicy.MaxFolderDepth)
                return Result<TopicResponse>.Fail(ErrorCodes.FolderDepthLimit);
        }

        var siblingType = await db.Topics
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.ParentId == request.ParentId)
            .Select(t => t.Type)
            .FirstOrDefaultAsync();

        if (siblingType is not null && siblingType != "topic")
            return Result<TopicResponse>.Fail(ErrorCodes.SiblingTypeMismatch);

        var folderCount = await db.Topics.CountAsync(t => t.UserId == userId && t.Type == "topic");
        if (folderCount >= TreePolicy.MaxFolders)
            return Result<TopicResponse>.Fail(ErrorCodes.FolderLimit);

        var childCount = await db.Topics.CountAsync(
            t => t.UserId == userId && t.ParentId == request.ParentId);
        if (childCount >= TreePolicy.MaxChildrenPerFolder)
            return Result<TopicResponse>.Fail(ErrorCodes.ChildrenLimit);

        var maxSortOrder = await db.Topics
            .Where(t => t.UserId == userId && t.ParentId == request.ParentId)
            .MaxAsync(t => (int?)t.SortOrder);

        var topic = new Topic
        {
            UserId = userId,
            ParentId = request.ParentId,
            Name = request.Name,
            Type = "topic",
            SortOrder = (maxSortOrder ?? -1) + 1
        };

        db.Topics.Add(topic);
        await db.SaveChangesAsync();

        return Result<TopicResponse>.Success(TopicResponse.From(topic));
    }

    public async Task<Result<TopicListResponse>> ListTopicsAsync(
        int userId,
        int? parentId,
        int? page,
        int? pageSize)
    {
        if (parentId is int id)
        {
            var parent = await db.Topics
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (parent is null)
                return Result<TopicListResponse>.Fail(ErrorCodes.TopicNotFound);

            if (parent.Type != "topic")
                return Result<TopicListResponse>.Fail(ErrorCodes.ParentTypeTaskNotAllowed);
        }

        var childrenQuery = db.Topics
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.ParentId == parentId)
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.Name);

        var totalCount = await childrenQuery.CountAsync();

        var childType = await db.Topics
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.ParentId == parentId)
            .Select(t => t.Type)
            .FirstOrDefaultAsync();

        List<Topic> topics;
        int responsePage;
        int responsePageSize;
        bool hasMore;

        if (page is null && pageSize is null)
        {
            topics = await childrenQuery
                .Take(TreePolicy.MaxChildrenPerFolder)
                .ToListAsync();

            responsePage = 1;
            responsePageSize = TreePolicy.MaxChildrenPerFolder;
            hasMore = false;
        }
        else
        {
            responsePage = page ?? 1;
            responsePageSize = pageSize ?? 20;

            if (responsePageSize is < 1 or > 50)
                return Result<TopicListResponse>.Fail(ErrorCodes.InvalidPageSize);

            if (responsePage < 1)
                return Result<TopicListResponse>.Fail(ErrorCodes.InvalidPage);

            topics = await childrenQuery
                .Skip((responsePage - 1) * responsePageSize)
                .Take(responsePageSize)
                .ToListAsync();

            hasMore = responsePage * responsePageSize < totalCount;
        }

        IReadOnlyList<TopicListItemDto> items = childType == "task"
            ? await BuildTaskListItemsAsync(topics)
            : topics.Select(TopicListItemDto.FromTopic).ToList();

        return Result<TopicListResponse>.Success(new TopicListResponse
        {
            ParentId = parentId,
            ChildType = childType,
            Items = items,
            TotalCount = page is null && pageSize is null ? items.Count : totalCount,
            Page = responsePage,
            PageSize = responsePageSize,
            HasMore = hasMore
        });
    }

    public async Task<Result<TopicSearchResult>> SearchAsync(
        int userId,
        string? query,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (query is null)
            return Result<TopicSearchResult>.Fail(ErrorCodes.InvalidQuery);

        if (pageSize is < 1 or > 50)
            return Result<TopicSearchResult>.Fail(ErrorCodes.InvalidPageSize);

        if (page < 1)
            return Result<TopicSearchResult>.Fail(ErrorCodes.InvalidPage);

        var result = await searchService.SearchAsync(
            userId,
            query,
            page,
            pageSize,
            cancellationToken);

        return Result<TopicSearchResult>.Success(result);
    }

    public async Task<Result<TaskStatsResult>> GetStatsAsync(
        int userId,
        int? topicId,
        DateTime? since,
        CancellationToken cancellationToken)
    {
        if (topicId is int id && id < 1)
            return Result<TaskStatsResult>.Fail(ErrorCodes.TopicNotFound);

        var result = await taskStatsService.GetStatsAsync(
            userId,
            topicId,
            since,
            cancellationToken);

        if (result is null)
            return Result<TaskStatsResult>.Fail(ErrorCodes.TopicNotFound);

        return Result<TaskStatsResult>.Success(result);
    }

    public async Task<Result<FolderTasksResponse>> ListFolderTasksAsync(
        int userId,
        int topicId,
        CancellationToken cancellationToken)
    {
        if (topicId < 1)
            return Result<FolderTasksResponse>.Fail(ErrorCodes.TopicNotFound);

        var topic = await db.Topics
            .AsNoTracking()
            .FirstOrDefaultAsync(
                t => t.Id == topicId && t.UserId == userId && t.Type == "topic",
                cancellationToken);

        if (topic is null)
            return Result<FolderTasksResponse>.Fail(ErrorCodes.TopicNotFound);

        var tasks = await TopicTreeHelper.CollectDescendantTaskTopicIdsAsync(
            db, userId, topicId, cancellationToken);

        if (tasks.Count == 0)
        {
            return Result<FolderTasksResponse>.Success(new FolderTasksResponse
            {
                TopicId = topicId,
                Items = []
            });
        }

        var parentIds = tasks
            .Where(t => t.ParentId is not null)
            .Select(t => t.ParentId!.Value)
            .Distinct()
            .ToList();

        var parentNames = await db.Topics
            .AsNoTracking()
            .Where(t => parentIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => t.Name, cancellationToken);

        var topicIds = tasks.Select(t => t.Id).ToList();

        var tasksInTask = await db.TaskItems
            .AsNoTracking()
            .Where(t => topicIds.Contains(t.TopicId))
            .ToDictionaryAsync(t => t.TopicId, cancellationToken);

        var items = tasks.Select(t =>
        {
            tasksInTask.TryGetValue(t.Id, out var task);
            var parentName = t.ParentId is int pid && parentNames.TryGetValue(pid, out var name)
                ? name
                : null;

            return TopicListItemDto.FromTask(t, task, parentName);
        }).ToList();

        return Result<FolderTasksResponse>.Success(new FolderTasksResponse
        {
            TopicId = topicId,
            Items = items
        });
    }

    public async Task<Result<DeleteSummaryResponse>> GetDeleteSummaryAsync(
        int userId,
        int topicId,
        CancellationToken cancellationToken)
    {
        if (topicId < 1)
            return Result<DeleteSummaryResponse>.Fail(ErrorCodes.TopicNotFound);

        var topic = await db.Topics
            .AsNoTracking()
            .FirstOrDefaultAsync(
                t => t.Id == topicId && t.UserId == userId && t.Type == "topic",
                cancellationToken);

        if (topic is null)
            return Result<DeleteSummaryResponse>.Fail(ErrorCodes.TopicNotFound);

        var (folderCount, taskCount) = await TopicTreeHelper.CountDescendantsAsync(
            db, userId, topic.Id);

        return Result<DeleteSummaryResponse>.Success(new DeleteSummaryResponse
        {
            TopicId = topic.Id,
            Name = topic.Name,
            FolderCount = folderCount,
            TaskCount = taskCount,
            TotalCount = folderCount + taskCount
        });
    }

    public async Task<Result> EmptyTopicAsync(
        int userId,
        int topicId,
        CancellationToken cancellationToken)
    {
        if (topicId < 1)
            return Result.Fail(ErrorCodes.TopicNotFound);

        var topic = await db.Topics.FirstOrDefaultAsync(
            t => t.Id == topicId && t.UserId == userId && t.Type == "topic",
            cancellationToken);

        if (topic is null)
            return Result.Fail(ErrorCodes.TopicNotFound);

        var children = await db.Topics
            .Where(t => t.UserId == userId && t.ParentId == topicId)
            .ToListAsync(cancellationToken);

        if (children.Count == 0)
            return Result.Success();

        db.Topics.RemoveRange(children);
        await db.SaveChangesAsync(cancellationToken);
        taskStatsService.Invalidate(userId, topicId);

        return Result.Success();
    }

    public async Task<Result> DeleteTopicAsync(
        int userId,
        int topicId,
        CancellationToken cancellationToken)
    {
        if (topicId < 1)
            return Result.Fail(ErrorCodes.TopicNotFound);

        var topic = await db.Topics.FirstOrDefaultAsync(
            t => t.Id == topicId && t.UserId == userId && t.Type == "topic",
            cancellationToken);

        if (topic is null)
            return Result.Fail(ErrorCodes.TopicNotFound);

        db.Topics.Remove(topic);
        await db.SaveChangesAsync(cancellationToken);
        taskStatsService.Invalidate(userId);

        return Result.Success();
    }

    public async Task<Result<TopicResponse>> PatchTopicAsync(
        int userId,
        int topicId,
        PatchTopicRequest request,
        CancellationToken cancellationToken)
    {
        if (!request.HasChanges)
            return Result<TopicResponse>.Fail(ErrorCodes.NoUpdateFields);

        if (topicId < 1)
            return Result<TopicResponse>.Fail(ErrorCodes.TopicNotFound);

        var topic = await db.Topics.FirstOrDefaultAsync(
            t => t.Id == topicId && t.UserId == userId,
            cancellationToken);

        if (topic is null)
            return Result<TopicResponse>.Fail(ErrorCodes.TopicNotFound);

        var previousParentId = topic.ParentId;

        if (request.Name is not null)
            topic.Name = request.Name;

        if (request.MoveParent)
        {
            var moveError = await TryMoveTopicAsync(userId, topic, request.ParentId, cancellationToken);
            if (moveError is not null)
                return Result<TopicResponse>.Fail(moveError);
        }

        if (request.Move is not null)
        {
            var reorderError = await TryReorderTopicAsync(userId, topic, request.Move, cancellationToken);
            if (reorderError is not null)
                return Result<TopicResponse>.Fail(reorderError);
        }

        await db.SaveChangesAsync(cancellationToken);
        taskStatsService.Invalidate(userId, topicId);
        if (previousParentId != topic.ParentId)
        {
            taskStatsService.Invalidate(userId, previousParentId);
            taskStatsService.Invalidate(userId, topic.ParentId);
        }

        return Result<TopicResponse>.Success(TopicResponse.From(topic));
    }

    private async Task<string?> TryMoveTopicAsync(
        int userId,
        Topic topic,
        int? newParentId,
        CancellationToken cancellationToken)
    {
        if (newParentId == topic.ParentId)
            return null;

        if (topic.Type == "topic" && newParentId == topic.Id)
            return ErrorCodes.MoveIntoSelf;

        if (topic.Type == "topic" && newParentId is int parentId &&
            await TopicTreeHelper.IsDescendantAsync(db, userId, topic.Id, parentId, cancellationToken))
            return ErrorCodes.MoveIntoDescendant;

        if (newParentId is int targetParentId)
        {
            var parent = await db.Topics
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == targetParentId && t.UserId == userId, cancellationToken);

            if (parent is null)
                return ErrorCodes.TopicNotFound;

            if (parent.Type != "topic")
                return ErrorCodes.ParentTypeTaskNotAllowed;
        }

        var siblingType = await db.Topics
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.ParentId == newParentId && t.Id != topic.Id)
            .Select(t => t.Type)
            .FirstOrDefaultAsync(cancellationToken);

        if (siblingType is not null && siblingType != topic.Type)
            return ErrorCodes.SiblingTypeMismatch;

        var childCount = await db.Topics.CountAsync(
            t => t.UserId == userId && t.ParentId == newParentId && t.Id != topic.Id,
            cancellationToken);

        if (childCount >= TreePolicy.MaxChildrenPerFolder)
            return ErrorCodes.ChildrenLimit;

        if (topic.Type == "topic")
        {
            var parentDepth = newParentId is int destinationId
                ? await GetDepthAsync(userId, destinationId, cancellationToken)
                : 0;
            var extraDepth = await TopicTreeHelper.GetDescendantFolderDepthAsync(
                db, userId, topic.Id, cancellationToken);

            if (parentDepth + extraDepth >= TreePolicy.MaxFolderDepth)
                return ErrorCodes.FolderDepthLimit;
        }

        var maxSortOrder = await db.Topics
            .Where(t => t.UserId == userId && t.ParentId == newParentId && t.Id != topic.Id)
            .MaxAsync(t => (int?)t.SortOrder, cancellationToken);

        topic.ParentId = newParentId;
        topic.SortOrder = (maxSortOrder ?? -1) + 1;
        return null;
    }

    private async Task<string?> TryReorderTopicAsync(
        int userId,
        Topic topic,
        string direction,
        CancellationToken cancellationToken)
    {
        var siblings = await db.Topics
            .Where(t => t.UserId == userId && t.ParentId == topic.ParentId)
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.Name)
            .ToListAsync(cancellationToken);

        var index = siblings.FindIndex(t => t.Id == topic.Id);
        if (index < 0)
            return ErrorCodes.TopicNotFound;

        var targetIndex = direction == "up" ? index - 1 : index + 1;
        if (targetIndex < 0 || targetIndex >= siblings.Count)
            return direction == "up" ? ErrorCodes.AlreadyAtTop : ErrorCodes.AlreadyAtBottom;

        var neighbor = siblings[targetIndex];
        (topic.SortOrder, neighbor.SortOrder) = (neighbor.SortOrder, topic.SortOrder);
        return null;
    }

    private async Task<List<TopicListItemDto>> BuildTaskListItemsAsync(List<Topic> topics)
    {
        var topicIds = topics.Select(t => t.Id).ToList();

        var tasks = await db.TaskItems
            .AsNoTracking()
            .Where(t => topicIds.Contains(t.TopicId))
            .ToDictionaryAsync(t => t.TopicId);

        return topics.Select(t =>
        {
            tasks.TryGetValue(t.Id, out var task);
            return TopicListItemDto.FromTask(t, task);
        }).ToList();
    }

    private async Task<int> GetDepthAsync(
        int userId,
        int topicId,
        CancellationToken cancellationToken = default)
    {
        var parentById = await db.Topics
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.Type == "topic")
            .ToDictionaryAsync(t => t.Id, t => t.ParentId, cancellationToken);

        var depth = 0;
        int? currentId = topicId;

        while (currentId is int id && parentById.ContainsKey(id))
        {
            depth++;
            if (depth > TreePolicy.MaxFolderDepth)
                break;

            currentId = parentById[id];
        }

        return depth;
    }
}
