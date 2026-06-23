namespace TaskManager.Models;

public enum TaskItemStatus
{
    Pending,
    Completed,
    Canceled
}

public class TaskItem
{
    public int TopicId { get; set; }

    public required string Description { get; set; }

    public TaskItemStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime? CanceledAt { get; set; }

    public Topic Topic { get; set; } = null!;
}
