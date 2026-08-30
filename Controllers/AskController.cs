using Microsoft.AspNetCore.Mvc;
using TaskManager.Auth;
using TaskManager.Dto.RequestsDto;
using TaskManager.Dto.ResponsesDto;
using TaskManager.Logging;
using TaskManager.Services;

namespace TaskManager.Controllers;

[ApiController]
[Route("api/ask")]
public class AskController(
    RagAccessService ragAccessService,
    RagClientService ragClientService,
    ILogger<AskController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Ask([FromBody] AskRequest request, CancellationToken cancellationToken)
    {
        var current = ClaimsHelper.GetAuthenticatedUser(User);
        var correlationId = Guid.NewGuid().ToString("N");

        logger.LogInformation(
            RagLogMessages.AskReceived,
            current.Id,
            request.Question.Length);

        var access = await ragAccessService.TryConsumeAsync(current, correlationId, cancellationToken);
        if (!access.Succeeded)
            return ErrorResults.From(access.Error!);

        logger.LogInformation(
            RagLogMessages.CallingPython,
            current.Id,
            correlationId);

        var result = await ragClientService.AskAsync(
            current.Id,
            request.Question,
            correlationId,
            cancellationToken);

        if (!result.Succeeded)
            return ErrorResults.From(result.Error!);

        return Ok(new AskResponse
        {
            Answer = result.Value!.Answer,
            Sources = result.Value.Sources
                .Select(s => new AskSourceDto { TopicId = s.TopicId, Name = s.Name })
                .ToList(),
            RemainingRequestsToday = access.Value
        });
    }
}
