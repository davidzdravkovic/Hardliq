using Microsoft.AspNetCore.Mvc;
using TaskManager.Auth;
using TaskManager.Dto.RequestsDto;
using TaskManager.Services;

namespace TaskManager.Controllers;

[ApiController]
[Route("topics")]
public class TopicsController(TopicService topicService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateTopicRequest request)
    {
        var current = ClaimsHelper.GetAuthenticatedUser(User);
        var result = await topicService.CreateTopicAsync(current.Id, request);

        if (!result.Succeeded)
            return ErrorResults.From(result.Error!);

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int? parentId,
        [FromQuery] int? page,
        [FromQuery] int? pageSize)
    {
        var current = ClaimsHelper.GetAuthenticatedUser(User);
        var result = await topicService.ListTopicsAsync(current.Id, parentId, page, pageSize);

        if (!result.Succeeded)
            return ErrorResults.From(result.Error!);

        return Ok(result.Value);
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var current = ClaimsHelper.GetAuthenticatedUser(User);

        var result = await topicService.SearchAsync(
            current.Id,
            q,
            page,
            pageSize,
            cancellationToken);

        if (!result.Succeeded)
            return ErrorResults.From(result.Error!);

        return Ok(result.Value);
    }

    [HttpGet("stats")]
    public async Task<IActionResult> Stats(
        [FromQuery] int? topicId,
        [FromQuery] DateTime? since,
        CancellationToken cancellationToken)
    {
        var current = ClaimsHelper.GetAuthenticatedUser(User);

        var result = await topicService.GetStatsAsync(
            current.Id,
            topicId,
            since,
            cancellationToken);

        if (!result.Succeeded)
            return ErrorResults.From(result.Error!);

        return Ok(result.Value);
    }

    [HttpGet("{topicId:int}/tasks")]
    public async Task<IActionResult> ListFolderTasks(int topicId, CancellationToken cancellationToken)
    {
        var current = ClaimsHelper.GetAuthenticatedUser(User);

        var result = await topicService.ListFolderTasksAsync(
            current.Id,
            topicId,
            cancellationToken);

        if (!result.Succeeded)
            return ErrorResults.From(result.Error!);

        return Ok(result.Value);
    }

    [HttpPatch("{topicId:int}")]
    public async Task<IActionResult> Patch(
        int topicId,
        [FromBody] PatchTopicRequest request,
        CancellationToken cancellationToken)
    {
        var current = ClaimsHelper.GetAuthenticatedUser(User);

        var result = await topicService.PatchTopicAsync(
            current.Id,
            topicId,
            request,
            cancellationToken);

        if (!result.Succeeded)
            return ErrorResults.From(result.Error!);

        return Ok(result.Value);
    }

    [HttpGet("{topicId:int}/delete-summary")]
    public async Task<IActionResult> DeleteSummary(int topicId, CancellationToken cancellationToken)
    {
        var current = ClaimsHelper.GetAuthenticatedUser(User);

        var result = await topicService.GetDeleteSummaryAsync(
            current.Id,
            topicId,
            cancellationToken);

        if (!result.Succeeded)
            return ErrorResults.From(result.Error!);

        return Ok(result.Value);
    }

    [HttpDelete("{topicId:int}/children")]
    public async Task<IActionResult> Empty(int topicId, CancellationToken cancellationToken)
    {
        var current = ClaimsHelper.GetAuthenticatedUser(User);

        var result = await topicService.EmptyTopicAsync(
            current.Id,
            topicId,
            cancellationToken);

        if (!result.Succeeded)
            return ErrorResults.From(result.Error!);

        return NoContent();
    }

    [HttpDelete("{topicId:int}")]
    public async Task<IActionResult> Delete(int topicId, CancellationToken cancellationToken)
    {
        var current = ClaimsHelper.GetAuthenticatedUser(User);

        var result = await topicService.DeleteTopicAsync(
            current.Id,
            topicId,
            cancellationToken);

        if (!result.Succeeded)
            return ErrorResults.From(result.Error!);

        return NoContent();
    }
}
