namespace TaskManager.Dto.ResponsesDto;

public class AskResponse
{
    public required string Answer { get; init; }
    public IReadOnlyList<AskSourceDto> Sources { get; init; } = [];
    public int RemainingRequestsToday { get; init; }
}

public class AskSourceDto
{
    public int TopicId { get; init; }
    public required string Name { get; init; }
}
