using TaskManager.Services;

namespace TaskManager.Dto.HelpersDto;

public class SearchItemDto
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public required string Type { get; init; }
    public int? ParentId { get; init; }
    public required IReadOnlyList<SearchPathSegmentDto> Path { get; init; }
    public string? Description { get; init; }
    public string? Status { get; init; }

    public static SearchItemDto From(TopicSearchItem item) => new()
    {
        Id = item.Id,
        Name = item.Name,
        Type = item.Type,
        ParentId = item.ParentId,
        Path = item.Path.Select(SearchPathSegmentDto.From).ToList(),
        Description = item.Description,
        Status = item.Status
    };
}
