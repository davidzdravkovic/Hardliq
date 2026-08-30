namespace TaskManager.Configuration;

public class RagOptions
{
    public const string SectionName = "Rag";

    public string BaseUrl { get; set; } = string.Empty;

    public string InternalKey { get; set; } = string.Empty;

    public int DailyLimitPerUser { get; set; } = 20;

    public string[] PremiumUsernames { get; set; } = [];

    public string[] UnlimitedUsernames { get; set; } = [];
}
