using TaskManager.Models;

namespace TaskManager.Dto.HelpersDto;

public class TopicListItemDto
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public required string Type { get; init; }
    public int? ParentId { get; init; }
    public int SortOrder { get; init; }
    public string? ParentName { get; init; }
    public string? Description { get; init; }
    public string? Status { get; init; }
    public DateTime? CreatedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public DateTime? CanceledAt { get; init; }

    public static TopicListItemDto FromTopic(Topic topic) => new()
    {
        Id = topic.Id,
        Name = topic.Name,
        Type = topic.Type,
        ParentId = topic.ParentId,
        SortOrder = topic.SortOrder
    };

    public static TopicListItemDto FromTask(Topic topic, TaskItem? task, string? parentName = null) => new()
    {
        Id = topic.Id,
        Name = topic.Name,
        Type = topic.Type,
        ParentId = topic.ParentId,
        SortOrder = topic.SortOrder,
        ParentName = parentName,
        Description = task?.Description,
        Status = task?.Status.ToString(),
        CreatedAt = task?.CreatedAt,
        CompletedAt = task?.CompletedAt,
        CanceledAt = task?.CanceledAt
    };
}
