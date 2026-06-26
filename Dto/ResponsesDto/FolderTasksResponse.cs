using TaskManager.Dto.HelpersDto;

namespace TaskManager.Dto.ResponsesDto;

public class FolderTasksResponse
{
    public int TopicId { get; init; }
    public required IReadOnlyList<TopicListItemDto> Items { get; init; }
}
