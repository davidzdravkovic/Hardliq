namespace TaskManager.Logging;

public static class RagLogMessages
{
    public const string AskReceived =
        "RAG ask received for user {UserId}: question length {QuestionLength}";

    public const string PremiumDenied =
        "RAG premium denied for user {UserId} ({Username}) CorrelationId={CorrelationId}";

    public const string QuotaDenied =
        "RAG quota denied for user {UserId}: count {Count}/{Limit} CorrelationId={CorrelationId}";

    public const string QuotaMisconfigured =
        "RAG quota denied for user {UserId}: daily limit misconfigured ({Limit}) CorrelationId={CorrelationId}";

    public const string QuotaConsumed =
        "RAG quota consumed for user {UserId}: {Remaining} remaining CorrelationId={CorrelationId}";

    public const string QuotaUnlimited =
        "RAG unlimited access for user {UserId} ({Username}) CorrelationId={CorrelationId}";

    public const string CallingPython =
        "RAG calling Python for user {UserId} CorrelationId={CorrelationId}";

    public const string PythonResponse =
        "RAG Python response for user {UserId}: {DurationMs}ms, {SourceCount} sources CorrelationId={CorrelationId}";

    public const string PythonFailure =
        "RAG Python failure for user {UserId} CorrelationId={CorrelationId} StatusCode={StatusCode}";

    public const string EmbedRequested =
        "RAG embed requested for topic {TopicId} StatusCode={StatusCode}";

    public const string EmbedFailure =
        "RAG embed failure for topic {TopicId} StatusCode={StatusCode}";
}
