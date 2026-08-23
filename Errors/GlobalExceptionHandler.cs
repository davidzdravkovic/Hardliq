using System.Security.Claims;
using Microsoft.AspNetCore.Diagnostics;
using TaskManager.Dto.ResponsesDto;

namespace TaskManager.Errors;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

        logger.LogError(
            exception,
            "Unhandled exception for {Method} {Path} (user {UserId})",
            httpContext.Request.Method,
            httpContext.Request.Path,
            userId ?? "anonymous");

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        await httpContext.Response.WriteAsJsonAsync(
            new MessageResponse { Message = "Something went wrong." },
            cancellationToken);

        return true;
    }
}
