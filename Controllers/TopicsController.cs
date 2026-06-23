using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManager.Auth;
using TaskManager.Data;
using TaskManager.Dto;
using TaskManager.Models;
using TaskManager.Services;

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
                return NotFound(new { message = "Parent topic not found." });

            if (parent.Type != "topic")
                return BadRequest(new { message = "Tasks cannot contain child topics." });
        }

        var siblingType = await db.Topics
            .AsNoTracking()
            .Where(t => t.UserId == current.Id && t.ParentId == request.ParentId)
            .Select(t => t.Type)
            .FirstOrDefaultAsync();

        if (siblingType is not null && siblingType != "topic")
            return BadRequest(new { message = "All siblings under the same parent must have the same type." });

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

        return StatusCode(StatusCodes.Status201Created, new
        {
            topic.Id,
            topic.Name,
            topic.Type,
            topic.ParentId,
            topic.SortOrder
        });
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int? parentId,
        [FromQuery] int pageSize = 20)
    {
        var current = ClaimsHelper.GetAuthenticatedUser(User);

        if (pageSize is < 1 or > 100)
            return BadRequest(new { message = "Page size must be between 1 and 100." });

        var topics = await db.Topics
            .AsNoTracking()
            .Where(t => t.UserId == current.Id && t.ParentId == parentId)
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.Name)
            .Take(pageSize)
            .ToListAsync();

        var childType = topics.FirstOrDefault()?.Type;
        
        var items = childType == "task"
            ? await BuildTaskListItemsAsync(topics)
            : topics.Select(t => (object)new
            {
                t.Id,
                t.Name,
                t.Type,
                t.ParentId,
                t.SortOrder
            }).ToList();

        return Ok(new { parentId, childType, items });
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
            return BadRequest(new { message = "Page must be at least 1." });

        if (pageSize is < 1 or > 50)
            return BadRequest(new { message = "Page size must be between 1 and 50." });

        var result = await searchService.SearchAsync(
            current.Id,
            q ?? string.Empty,
            page,
            pageSize,
            cancellationToken);

        return Ok(new
        {
            query = result.Query,
            page = result.Page,
            pageSize = result.PageSize,
            totalCount = result.TotalCount,
            totalPages = result.TotalPages,
            hasMore = result.HasMore,
            items = result.Items.Select(i => new
            {
                i.Id,
                i.Name,
                i.Type,
                i.ParentId,
                path = i.Path.Select(p => new { p.Id, p.Name }),
                description = i.Description,
                status = i.Status
            })
        });
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

            return Ok(new
            {
                scope = stats.Scope,
                topicId = stats.TopicId,
                topicName = stats.TopicName,
                totalTasks = stats.TotalTasks,
                pending = stats.Pending,
                completed = stats.Completed,
                canceled = stats.Canceled,
                executedSince = stats.ExecutedSince,
                completedSince = stats.CompletedSince,
                canceledSince = stats.CanceledSince
            });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Topic not found." });
        }
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
            return NotFound(new { message = "Topic not found." });

        var (folderCount, taskCount) = await TopicTreeHelper.CountDescendantsAsync(db, current.Id, topicId);

        return Ok(new
        {
            topicId = topic.Id,
            name = topic.Name,
            folderCount,
            taskCount,
            totalCount = folderCount + taskCount
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
            return NotFound(new { message = "Topic not found." });

        db.Topics.Remove(topic);
        await db.SaveChangesAsync();
        statsService.Invalidate(current.Id);

        return NoContent();
    }

    private async Task<List<object>> BuildTaskListItemsAsync(List<Topic> topics)
    {
        var topicIds = topics.Select(t => t.Id).ToList();

        var tasks = await db.TaskItems
            .AsNoTracking()
            .Where(t => topicIds.Contains(t.TopicId))
            .ToDictionaryAsync(t => t.TopicId);

        return topics.Select(t =>
        {
            tasks.TryGetValue(t.Id, out var task);

            return (object)new
            {
                t.Id,
                t.Name,
                t.Type,
                t.ParentId,
                t.SortOrder,
                description = task?.Description,
                status = task?.Status.ToString(),
                createdAt = task?.CreatedAt,
                completedAt = task?.CompletedAt,
                canceledAt = task?.CanceledAt
            };
        }).ToList();
    }
}
