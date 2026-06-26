using TaskManager.Models;

namespace TaskManager.Dto.ResponsesDto;

public class TaskDetailResponse
{
    public int TopicId { get; init; }
    public required string Description { get; init; }
    public required string Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public DateTime? CanceledAt { get; init; }

    public static TaskDetailResponse From(TaskItem task) => new()
    {
        TopicId = task.TopicId,
        Description = task.Description,
        Status = task.Status.ToString(),
        CreatedAt = task.CreatedAt,
        CompletedAt = task.CompletedAt,
        CanceledAt = task.CanceledAt
    };
}
