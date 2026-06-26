namespace TaskManager.Dto.ResponsesDto;

public class DeleteSummaryResponse
{
    public int TopicId { get; init; }
    public required string Name { get; init; }
    public int FolderCount { get; init; }
    public int TaskCount { get; init; }
    public int TotalCount { get; init; }
}
