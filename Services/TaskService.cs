using TaskManager.Data;
using TaskManager.Domain;
using TaskManager.Dto.RequestsDto;
using TaskManager.Dto.ResponsesDto;
using Microsoft.EntityFrameworkCore;
using TaskManager.Errors;
using TaskManager.Models;

namespace TaskManager.Services;


public class TaskService(AppDbContext db, TaskStatsService taskStatsService)
{
    public async Task<Result<CreateTaskResponse>> CreateTaskAsync (int userId, int parentId, CreateTaskRequest request )
    {
    var parent = await db.Topics
          .AsNoTracking()
          .FirstOrDefaultAsync(t => t.Id == parentId && t.UserId == userId);

     if (parent is null)
            return Result<CreateTaskResponse>.Fail(ErrorCodes.TopicNotFound);

        if (parent.Type != "topic")
            return Result<CreateTaskResponse>.Fail(ErrorCodes.ParentTypeTaskNotAllowed);

        var siblingType = await db.Topics
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.ParentId == parentId)
            .Select(t => t.Type)
            .FirstOrDefaultAsync();

        if (siblingType is not null && siblingType != "task")
            return Result<CreateTaskResponse>.Fail(ErrorCodes.SiblingTypeMismatch);

        var childCount = await db.Topics.CountAsync(
            t => t.UserId == userId && t.ParentId == parentId);
        if (childCount >= TreePolicy.MaxChildrenPerFolder)
            return Result<CreateTaskResponse>.Fail(ErrorCodes.ChildrenLimit);

        var maxSortOrder = await db.Topics
            .Where(t => t.UserId == userId && t.ParentId == parentId)
            .MaxAsync(t => (int?)t.SortOrder);

        var topic = new Topic
        {
            UserId = userId,
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
        taskStatsService.Invalidate(userId);
        return Result<CreateTaskResponse>.Success(CreateTaskResponse.From(topic, task));
    }

    public async Task<Result<TaskDetailResponse>> UpdateTaskAsync(
        int userId,
        int topicId,
        string? description,
        TaskItemStatus? status)
    {
        if (description is null && status is null)
            return Result<TaskDetailResponse>.Fail(ErrorCodes.NoUpdateFields);

        var topic = await db.Topics.FirstOrDefaultAsync(t =>
            t.Id == topicId &&
            t.UserId == userId &&
            t.Type == "task");

        if (topic is null)
            return Result<TaskDetailResponse>.Fail(ErrorCodes.TaskNotFound);

        var task = await db.TaskItems.FirstOrDefaultAsync(t => t.TopicId == topicId);
        if (task is null)
            return Result<TaskDetailResponse>.Fail(ErrorCodes.TaskNotFound);

        if (description is not null)
            task.Description = description;

        if (status is not null)
        {
            if (status is TaskItemStatus.Pending)
                return Result<TaskDetailResponse>.Fail(ErrorCodes.StatusPendingNotAllowed);

            ApplyStatusChange(task, status.Value);
        }

        await db.SaveChangesAsync();
        taskStatsService.Invalidate(userId);
        return Result<TaskDetailResponse>.Success(TaskDetailResponse.From(task));
    }

    public async Task<Result> DeleteTaskAsync(int userId, int topicId)
    {
        var topic = await db.Topics.FirstOrDefaultAsync(t =>
            t.Id == topicId &&
            t.UserId == userId &&
            t.Type == "task");

        if (topic is null)
            return Result.Fail(ErrorCodes.TaskNotFound);

        db.Topics.Remove(topic);
        await db.SaveChangesAsync();
        taskStatsService.Invalidate(userId);
        return Result.Success();
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