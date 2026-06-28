using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManager.Auth;
using TaskManager.Data;
using TaskManager.Dto.HelpersDto;
using TaskManager.Dto.RequestsDto;
using TaskManager.Dto.ResponsesDto;
using TaskManager.Models;
using TaskManager.Services;
using TaskManager.Utilities;

namespace TaskManager.Controllers;

[ApiController]
[Route("topics")]
[Authorize]
public class TopicsController(AppDbContext db, TaskStatsService statsService, TopicSearchService searchService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTopicRequest request)
    {
        var current = ClaimsHelper.GetAuthenticatedUser(User);

        if (request.ParentId is int parentId)
        {
            var parent = await db.Topics
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == parentId && t.UserId == current.Id);

            if (parent is null)
                return NotFound(new MessageResponse { Message = "Parent topic not found." });

            if (parent.Type != "topic")
                return BadRequest(new MessageResponse { Message = "Tasks cannot contain child topics." });
        }

        var siblingType = await db.Topics
            .AsNoTracking()
            .Where(t => t.UserId == current.Id && t.ParentId == request.ParentId)
            .Select(t => t.Type)
            .FirstOrDefaultAsync();

        if (siblingType is not null && siblingType != "topic")
            return BadRequest(new MessageResponse { Message = "All siblings under the same parent must have the same type." });

        var maxSortOrder = await db.Topics
            .Where(t => t.UserId == current.Id && t.ParentId == request.ParentId)
            .MaxAsync(t => (int?)t.SortOrder);

        var topic = new Topic
        {
            UserId = current.Id,
            ParentId = request.ParentId,
            Name = request.Name,
            Type = "topic",
            SortOrder = (maxSortOrder ?? -1) + 1
        };

        db.Topics.Add(topic);
        await db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, TopicResponse.From(topic));
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int? parentId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var current = ClaimsHelper.GetAuthenticatedUser(User);

        if (page < 1)
            return BadRequest(new MessageResponse { Message = "Page must be at least 1." });

        if (pageSize is < 1 or > 100)
            return BadRequest(new MessageResponse { Message = "Page size must be between 1 and 100." });

        var query = db.Topics
            .AsNoTracking()
            .Where(t => t.UserId == current.Id && t.ParentId == parentId);

        var totalCount = await query.CountAsync();

        var topics = await query
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var childType = topics.FirstOrDefault()?.Type;

        IReadOnlyList<TopicListItemDto> items = childType == "task"
            ? await BuildTaskListItemsAsync(topics)
            : topics.Select(TopicListItemDto.FromTopic).ToList();

        return Ok(new TopicListResponse
        {
            ParentId = parentId,
            ChildType = childType,
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            HasMore = page * pageSize < totalCount
        });
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var current = ClaimsHelper.GetAuthenticatedUser(User);

        if (page < 1)
            return BadRequest(new MessageResponse { Message = "Page must be at least 1." });

        if (pageSize is < 1 or > 50)
            return BadRequest(new MessageResponse { Message = "Page size must be between 1 and 50." });

        var result = await searchService.SearchAsync(
            current.Id,
            q ?? string.Empty,
            page,
            pageSize,
            cancellationToken);

        return Ok(SearchResponse.From(result));
    }

    [HttpGet("stats")]
    public async Task<IActionResult> Stats(
        [FromQuery] int? topicId,
        [FromQuery] DateTime? since,
        CancellationToken cancellationToken)
    {
        var current = ClaimsHelper.GetAuthenticatedUser(User);

        try
        {
            var stats = await statsService.GetStatsAsync(
                current.Id,
                topicId,
                since,
                cancellationToken);

            return Ok(StatsResponse.From(stats));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new MessageResponse { Message = "Topic not found." });
        }
    }

    [HttpGet("{topicId:int}/tasks")]
    public async Task<IActionResult> ListFolderTasks(int topicId, CancellationToken cancellationToken)
    {
        var current = ClaimsHelper.GetAuthenticatedUser(User);

        var topic = await db.Topics
            .AsNoTracking()
            .FirstOrDefaultAsync(
                t => t.Id == topicId && t.UserId == current.Id && t.Type == "topic",
                cancellationToken);

        if (topic is null)
            return NotFound(new MessageResponse { Message = "Topic not found." });

        var tasks = await TopicTreeHelper.CollectDescendantTaskTopicIdsAsync(
            db, current.Id, topicId, cancellationToken);

        if (tasks.Count == 0)
        {
            return Ok(new FolderTasksResponse
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

        return Ok(new FolderTasksResponse
        {
            TopicId = topicId,
            Items = items
        });
    }

    [HttpPatch("{topicId:int}")]
    public async Task<IActionResult> Patch(int topicId, [FromBody] PatchTopicRequest request)
    {
        if (!request.HasChanges)
            return BadRequest(new MessageResponse { Message = "Provide name, moveParent, and/or move to update." });

        var current = ClaimsHelper.GetAuthenticatedUser(User);

        var topic = await db.Topics.FirstOrDefaultAsync(t =>
            t.Id == topicId && t.UserId == current.Id);

        if (topic is null)
            return NotFound(new MessageResponse { Message = "Topic not found." });

        var previousParentId = topic.ParentId;

        if (request.Name is not null)
            topic.Name = request.Name;

        if (request.MoveParent)
        {
            var moveError = await TryMoveTopicAsync(current.Id, topic, request.ParentId);
            if (moveError is not null)
                return BadRequest(new MessageResponse { Message = moveError });
        }

        if (request.Move is not null)
        {
            var reorderError = await TryReorderTopicAsync(current.Id, topic, request.Move);
            if (reorderError is not null)
                return BadRequest(new MessageResponse { Message = reorderError });
        }

        await db.SaveChangesAsync();
        statsService.Invalidate(current.Id, topicId);
        if (previousParentId != topic.ParentId)
        {
            statsService.Invalidate(current.Id, previousParentId);
            statsService.Invalidate(current.Id, topic.ParentId);
        }

        return Ok(TopicResponse.From(topic));
    }

    [HttpGet("{topicId:int}/delete-summary")]
    public async Task<IActionResult> DeleteSummary(int topicId)
    {
        var current = ClaimsHelper.GetAuthenticatedUser(User);

        var topic = await db.Topics
            .AsNoTracking()
            .FirstOrDefaultAsync(t =>
                t.Id == topicId &&
                t.UserId == current.Id &&
                t.Type == "topic");

        if (topic is null)
            return NotFound(new MessageResponse { Message = "Topic not found." });

        var (folderCount, taskCount) = await TopicTreeHelper.CountDescendantsAsync(db, current.Id, topic.Id);

        return Ok(new DeleteSummaryResponse
        {
            TopicId = topic.Id,
            Name = topic.Name,
            FolderCount = folderCount,
            TaskCount = taskCount,
            TotalCount = folderCount + taskCount
        });
    }

    [HttpDelete("{topicId:int}/children")]
    public async Task<IActionResult> Empty(int topicId)
    {
        var current = ClaimsHelper.GetAuthenticatedUser(User);

        var topic = await db.Topics.FirstOrDefaultAsync(t =>
            t.Id == topicId &&
            t.UserId == current.Id &&
            t.Type == "topic");

        if (topic is null)
            return NotFound(new MessageResponse { Message = "Topic not found." });

        var children = await db.Topics
            .Where(t => t.UserId == current.Id && t.ParentId == topicId)
            .ToListAsync();

        if (children.Count == 0)
            return NoContent();

        db.Topics.RemoveRange(children);
        await db.SaveChangesAsync();
        statsService.Invalidate(current.Id, topicId);

        return NoContent();
    }

    [HttpDelete("{topicId:int}")]
    public async Task<IActionResult> Delete(int topicId)
    {
        var current = ClaimsHelper.GetAuthenticatedUser(User);

        var topic = await db.Topics.FirstOrDefaultAsync(t =>
            t.Id == topicId &&
            t.UserId == current.Id &&
            t.Type == "topic");

        if (topic is null)
            return NotFound(new MessageResponse { Message = "Topic not found." });

        db.Topics.Remove(topic);
        await db.SaveChangesAsync();
        statsService.Invalidate(current.Id);

        return NoContent();
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

    private async Task<string?> TryMoveTopicAsync(int userId, Topic topic, int? newParentId)
    {
        if (newParentId == topic.ParentId)
            return null;

        if (topic.Type == "topic" && newParentId == topic.Id)
            return "A folder cannot be moved into itself.";

        if (topic.Type == "topic" && newParentId is int parentId &&
            await TopicTreeHelper.IsDescendantAsync(db, userId, topic.Id, parentId))
            return "A folder cannot be moved into its own subfolder.";

        if (newParentId is int targetParentId)
        {
            var parent = await db.Topics
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == targetParentId && t.UserId == userId);

            if (parent is null)
                return "Parent topic not found.";

            if (parent.Type != "topic")
                return "Tasks cannot be moved under another task.";
        }

        var siblingType = await db.Topics
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.ParentId == newParentId && t.Id != topic.Id)
            .Select(t => t.Type)
            .FirstOrDefaultAsync();

        if (siblingType is not null && siblingType != topic.Type)
            return "All siblings under the same parent must have the same type.";

        var maxSortOrder = await db.Topics
            .Where(t => t.UserId == userId && t.ParentId == newParentId && t.Id != topic.Id)
            .MaxAsync(t => (int?)t.SortOrder);

        topic.ParentId = newParentId;
        topic.SortOrder = (maxSortOrder ?? -1) + 1;
        return null;
    }

    private async Task<string?> TryReorderTopicAsync(int userId, Topic topic, string direction)
    {
        var siblings = await db.Topics
            .Where(t => t.UserId == userId && t.ParentId == topic.ParentId)
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.Name)
            .ToListAsync();

        var index = siblings.FindIndex(t => t.Id == topic.Id);
        if (index < 0)
            return "Topic not found among siblings.";

        var targetIndex = direction == "up" ? index - 1 : index + 1;
        if (targetIndex < 0 || targetIndex >= siblings.Count)
            return direction == "up" ? "Already at the top." : "Already at the bottom.";

        var neighbor = siblings[targetIndex];
        (topic.SortOrder, neighbor.SortOrder) = (neighbor.SortOrder, topic.SortOrder);
        return null;
    }
}
