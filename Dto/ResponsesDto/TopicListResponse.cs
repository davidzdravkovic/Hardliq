using TaskManager.Dto.HelpersDto;

namespace TaskManager.Dto.ResponsesDto;

public class TopicListResponse
{
    public int? ParentId { get; init; }
    public string? ChildType { get; init; }
    public required IReadOnlyList<TopicListItemDto> Items { get; init; }
}
