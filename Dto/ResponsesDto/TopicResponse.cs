using TaskManager.Models;

namespace TaskManager.Dto.ResponsesDto;

public class TopicResponse
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public required string Type { get; init; }
    public int? ParentId { get; init; }
    public int SortOrder { get; init; }

    public static TopicResponse From(Topic topic) => new()
    {
        Id = topic.Id,
        Name = topic.Name,
        Type = topic.Type,
        ParentId = topic.ParentId,
        SortOrder = topic.SortOrder
    };
}
