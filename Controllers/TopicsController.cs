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
        [FromQuery] int pageSize = 20)
    {
        var current = ClaimsHelper.GetAuthenticatedUser(User);

        if (pageSize is < 1 or > 100)
            return BadRequest(new MessageResponse { Message = "Page size must be between 1 and 100." });

        var topics = await db.Topics
            .AsNoTracking()
            .Where(t => t.UserId == current.Id && t.ParentId == parentId)
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.Name)
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
            Items = items
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
}
