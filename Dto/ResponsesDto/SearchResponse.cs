using TaskManager.Dto.HelpersDto;
using TaskManager.Services;

namespace TaskManager.Dto.ResponsesDto;

public class SearchResponse
{
    public required string Query { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
    public bool HasMore { get; init; }
    public required IReadOnlyList<SearchItemDto> Items { get; init; }

    public static SearchResponse From(TopicSearchResult result) => new()
    {
        Query = result.Query,
        Page = result.Page,
        PageSize = result.PageSize,
        TotalCount = result.TotalCount,
        TotalPages = result.TotalPages,
        HasMore = result.HasMore,
        Items = result.Items.Select(SearchItemDto.From).ToList()
    };
}
