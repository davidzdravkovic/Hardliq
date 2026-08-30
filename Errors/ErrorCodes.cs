namespace TaskManager.Errors;

public static class ErrorCodes
{
    public const string InvalidCredentials = "invalid-credentials";
    public const string UsernameTaken = "username-taken";
    public const string EmailTaken = "email-taken";

    public const string TopicNotFound = "topic-not-found";
    public const string TaskNotFound = "task-not-found";
    public const string NoUpdateFields = "no-update-fields";
    public const string StatusPendingNotAllowed = "status-pending-not-allowed";

    public const string ParentTypeTaskNotAllowed = "parent-type-task-not-allowed";
    public const string SiblingTypeMismatch = "sibling-type-mismatch";
    public const string FolderLimit = "folder-limit";
    public const string ChildrenLimit = "children-limit";
    public const string FolderDepthLimit = "folder-depth-limit";

    public const string InvalidQuery = "invalid-search-query";
    public const string InvalidPageSize = "page-size-must-be-1-50-range";
    public const string InvalidPage = "page-must-be-at-least-1";

    public const string MoveIntoSelf = "folder-cannot-move-into-itself";
    public const string MoveIntoDescendant = "folder-cannot-move-into-subfolder";
    public const string AlreadyAtTop = "already-at-the-top";
    public const string AlreadyAtBottom = "already-at-the-bottom";

    public const string PremiumRequired = "premium-required";
    public const string RagDailyLimitReached = "rag-daily-limit-reached";
    public const string RagServiceUnavailable = "rag-service-unavailable";
}
