using TaskManager.Services;

namespace TaskManager.Dto.HelpersDto;

public class SearchPathSegmentDto
{
    public int Id { get; init; }
    public required string Name { get; init; }

    public static SearchPathSegmentDto From(TopicPathSegment segment) => new()
    {
        Id = segment.Id,
        Name = segment.Name
    };
}
