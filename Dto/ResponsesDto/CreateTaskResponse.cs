using TaskManager.Models;

namespace TaskManager.Dto.ResponsesDto;

public class CreateTaskResponse
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public required string Type { get; init; }
    public int? ParentId { get; init; }
    public int SortOrder { get; init; }
    public required string Description { get; init; }
    public required string Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public DateTime? CanceledAt { get; init; }

    public static CreateTaskResponse From(Topic topic, TaskItem task) => new()
    {
        Id = topic.Id,
        Name = topic.Name,
        Type = topic.Type,
        ParentId = topic.ParentId,
        SortOrder = topic.SortOrder,
        Description = task.Description,
        Status = task.Status.ToString(),
        CreatedAt = task.CreatedAt,
        CompletedAt = task.CompletedAt,
        CanceledAt = task.CanceledAt
    };
}
