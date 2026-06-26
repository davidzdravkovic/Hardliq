using TaskManager.Services;

namespace TaskManager.Dto.ResponsesDto;

public class StatsResponse
{
    public required string Scope { get; init; }
    public int? TopicId { get; init; }
    public string? TopicName { get; init; }
    public int TotalTasks { get; init; }
    public int Pending { get; init; }
    public int Completed { get; init; }
    public int Canceled { get; init; }
    public DateTime ExecutedSince { get; init; }
    public int CompletedSince { get; init; }
    public int CanceledSince { get; init; }

    public static StatsResponse From(TaskStatsResult stats) => new()
    {
        Scope = stats.Scope,
        TopicId = stats.TopicId,
        TopicName = stats.TopicName,
        TotalTasks = stats.TotalTasks,
        Pending = stats.Pending,
        Completed = stats.Completed,
        Canceled = stats.Canceled,
        ExecutedSince = stats.ExecutedSince,
        CompletedSince = stats.CompletedSince,
        CanceledSince = stats.CanceledSince
    };
}
