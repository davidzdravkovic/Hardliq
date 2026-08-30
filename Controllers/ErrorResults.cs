using Microsoft.AspNetCore.Mvc;
using TaskManager.Dto.ResponsesDto;
using TaskManager.Errors;

namespace TaskManager.Controllers;

public static class ErrorResults
{
    public static IActionResult From(string error) => error switch
    {
        ErrorCodes.InvalidCredentials => new UnauthorizedObjectResult(Body(error)),
        ErrorCodes.UsernameTaken or ErrorCodes.EmailTaken => new ConflictObjectResult(Body(error)),
        ErrorCodes.PremiumRequired => new ObjectResult(Body(error)) { StatusCode = StatusCodes.Status403Forbidden },
        ErrorCodes.RagDailyLimitReached => new ObjectResult(Body(error)) { StatusCode = StatusCodes.Status429TooManyRequests },
        ErrorCodes.RagServiceUnavailable => new ObjectResult(Body(error)) { StatusCode = StatusCodes.Status503ServiceUnavailable },
        ErrorCodes.TopicNotFound or ErrorCodes.TaskNotFound => new NotFoundObjectResult(Body(error)),
        ErrorCodes.ParentTypeTaskNotAllowed
            or ErrorCodes.SiblingTypeMismatch
            or ErrorCodes.NoUpdateFields
            or ErrorCodes.StatusPendingNotAllowed
            or ErrorCodes.FolderLimit
            or ErrorCodes.ChildrenLimit
            or ErrorCodes.FolderDepthLimit
            or ErrorCodes.InvalidPage
            or ErrorCodes.InvalidPageSize
            or ErrorCodes.InvalidQuery
            or ErrorCodes.MoveIntoSelf
            or ErrorCodes.MoveIntoDescendant
            or ErrorCodes.AlreadyAtTop
            or ErrorCodes.AlreadyAtBottom
            => new BadRequestObjectResult(Body(error)),
        _ => new BadRequestObjectResult(Body(error))
    };

    private static MessageResponse Body(string error) => new() { Message = ToMessage(error) };

    private static string ToMessage(string error) => error switch
    {
        ErrorCodes.InvalidCredentials => "Invalid username or password.",
        ErrorCodes.UsernameTaken => "That username is already taken.",
        ErrorCodes.EmailTaken => "That email address is already in use.",
        ErrorCodes.TopicNotFound => "Topic not found.",
        ErrorCodes.TaskNotFound => "Task not found.",
        ErrorCodes.NoUpdateFields => "Provide at least one field to update.",
        ErrorCodes.StatusPendingNotAllowed => "A task cannot be set back to pending.",
        ErrorCodes.ParentTypeTaskNotAllowed => "Items can only be placed under a folder.",
        ErrorCodes.SiblingTypeMismatch => "All items under the same folder must be the same type.",
        ErrorCodes.FolderLimit => "You have reached the maximum number of folders.",
        ErrorCodes.ChildrenLimit => "This folder has reached the maximum number of items.",
        ErrorCodes.FolderDepthLimit => "This folder is already at the maximum depth.",
        ErrorCodes.InvalidQuery => "Search query is required.",
        ErrorCodes.InvalidPageSize => "Page size must be between 1 and 50.",
        ErrorCodes.InvalidPage => "Page must be at least 1.",
        ErrorCodes.MoveIntoSelf => "A folder cannot be moved into itself.",
        ErrorCodes.MoveIntoDescendant => "A folder cannot be moved into its own subfolder.",
        ErrorCodes.AlreadyAtTop => "Already at the top.",
        ErrorCodes.AlreadyAtBottom => "Already at the bottom.",
        ErrorCodes.PremiumRequired => "Premium access is required to use AI ask.",
        ErrorCodes.RagDailyLimitReached => "You have reached your daily AI ask limit.",
        ErrorCodes.RagServiceUnavailable => "AI service is temporarily unavailable.",
        _ => error
    };
}
