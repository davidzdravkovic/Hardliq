using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManager.Auth;
using TaskManager.Data;
using TaskManager.Dto.RequestsDto;
using TaskManager.Dto.ResponsesDto;
using TaskManager.Models;
using TaskManager.Services;

namespace TaskManager.Controllers;

[ApiController]
[Route("topics")]
[Authorize]
public class TasksController(AppDbContext db, TaskStatsService statsService) : ControllerBase
{
    [HttpPost("{parentId:int}/tasks")]
    public async Task<IActionResult> Create(int parentId, [FromBody] CreateTaskRequest request)
    {
        var current = ClaimsHelper.GetAuthenticatedUser(User);

        var parent = await db.Topics
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == parentId && t.UserId == current.Id);

        if (parent is null)
            return NotFound(new MessageResponse { Message = "Parent topic not found." });

        if (parent.Type != "topic")
            return BadRequest(new MessageResponse { Message = "Tasks cannot be created under a task." });

        var siblingType = await db.Topics
            .AsNoTracking()
            .Where(t => t.UserId == current.Id && t.ParentId == parentId)
            .Select(t => t.Type)
            .FirstOrDefaultAsync();

        if (siblingType is not null && siblingType != "task")
            return BadRequest(new MessageResponse { Message = "All siblings under the same parent must have the same type." });

        var maxSortOrder = await db.Topics
            .Where(t => t.UserId == current.Id && t.ParentId == parentId)
            .MaxAsync(t => (int?)t.SortOrder);

        var topic = new Topic
        {
            UserId = current.Id,
            ParentId = parentId,
            Name = request.Name,
            Type = "task",
            SortOrder = (maxSortOrder ?? -1) + 1
        };

        var task = new TaskItem
        {
            Topic = topic,
            Description = request.Description,
            Status = request.Status,
            CreatedAt = DateTime.UtcNow
        };

        db.Topics.Add(topic);
        db.TaskItems.Add(task);
        await db.SaveChangesAsync();
        statsService.Invalidate(current.Id);

        return StatusCode(StatusCodes.Status201Created, CreateTaskResponse.From(topic, task));
    }

    [HttpPut("{topicId:int}/task")]
    public async Task<IActionResult> Put(int topicId, [FromBody] PutTaskRequest request)
    {
        var current = ClaimsHelper.GetAuthenticatedUser(User);
        var topic = await FindTaskTopicAsync(topicId);

        if (topic is null)
            return NotFound(new MessageResponse { Message = "Task not found." });

        var task = await db.TaskItems.FirstOrDefaultAsync(t => t.TopicId == topicId);
        if (task is null)
            return NotFound(new MessageResponse { Message = "Task details not found." });

        if (request.Status is TaskItemStatus.Pending)
            return BadRequest(new MessageResponse { Message = "Status can only be updated to Completed or Canceled." });

        task.Description = request.Description;
        ApplyStatusChange(task, request.Status);

        await db.SaveChangesAsync();
        statsService.Invalidate(current.Id);

        return Ok(TaskDetailResponse.From(task));
    }

    [HttpPatch("{topicId:int}/task")]
    public async Task<IActionResult> Patch(int topicId, [FromBody] PatchTaskRequest request)
    {
        if (request.Description is null && request.Status is null)
            return BadRequest(new MessageResponse { Message = "Provide description and/or status to update." });

        var current = ClaimsHelper.GetAuthenticatedUser(User);
        var topic = await FindTaskTopicAsync(topicId);
        if (topic is null)
            return NotFound(new MessageResponse { Message = "Task not found." });

        var task = await db.TaskItems.FirstOrDefaultAsync(t => t.TopicId == topicId);
        if (task is null)
            return NotFound(new MessageResponse { Message = "Task details not found." });

        if (request.Description is not null)
            task.Description = request.Description;

        if (request.Status is not null)
        {
            if (request.Status is TaskItemStatus.Pending)
                return BadRequest(new MessageResponse { Message = "Status can only be updated to Completed or Canceled." });

            ApplyStatusChange(task, request.Status.Value);
        }

        await db.SaveChangesAsync();
        statsService.Invalidate(current.Id);

        return Ok(TaskDetailResponse.From(task));
    }

    [HttpDelete("{topicId:int}/task")]
    public async Task<IActionResult> Delete(int topicId)
    {
        var current = ClaimsHelper.GetAuthenticatedUser(User);

        var topic = await db.Topics.FirstOrDefaultAsync(t =>
            t.Id == topicId &&
            t.UserId == current.Id &&
            t.Type == "task");

        if (topic is null)
            return NotFound(new MessageResponse { Message = "Task not found." });

        db.Topics.Remove(topic);
        await db.SaveChangesAsync();
        statsService.Invalidate(current.Id);

        return NoContent();
    }

    private async Task<Topic?> FindTaskTopicAsync(int topicId)
    {
        var current = ClaimsHelper.GetAuthenticatedUser(User);

        return await db.Topics
            .AsNoTracking()
            .FirstOrDefaultAsync(t =>
                t.Id == topicId &&
                t.UserId == current.Id &&
                t.Type == "task");
    }

    private static void ApplyStatusChange(TaskItem task, TaskItemStatus newStatus)
    {
        if (task.Status == newStatus)
            return;

        task.Status = newStatus;
        var now = DateTime.UtcNow;

        if (newStatus == TaskItemStatus.Completed)
        {
            task.CompletedAt = now;
            task.CanceledAt = null;
        }
        else if (newStatus == TaskItemStatus.Canceled)
        {
            task.CanceledAt = now;
            task.CompletedAt = null;
        }
    }
}
