namespace TaskManager.Models;

public class RagDailyUsage
{
    public int UserId { get; set; }

    public DateOnly UsageDate { get; set; }

    public int RequestCount { get; set; }

    public User User { get; set; } = null!;
}
