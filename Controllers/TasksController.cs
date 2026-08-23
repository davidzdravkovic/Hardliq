using Microsoft.AspNetCore.Mvc;
using TaskManager.Auth;
using TaskManager.Dto.RequestsDto;
using TaskManager.Services;

namespace TaskManager.Controllers;

[ApiController]
[Route("topics")]
public class TasksController(TaskService taskService) : ControllerBase
{
    [HttpPost("{parentId:int}/tasks")]
    public async Task<IActionResult> Create(int parentId, [FromBody] CreateTaskRequest request)
    {
        var current = ClaimsHelper.GetAuthenticatedUser(User);
        var result = await taskService.CreateTaskAsync(current.Id, parentId, request);

        if (!result.Succeeded)
            return ErrorResults.From(result.Error!);

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    [HttpPut("{topicId:int}/task")]
    public async Task<IActionResult> Put(int topicId, [FromBody] PutTaskRequest request)
    {
        var current = ClaimsHelper.GetAuthenticatedUser(User);
        var result = await taskService.UpdateTaskAsync(
            current.Id, topicId, request.Description, request.Status);

        if (!result.Succeeded)
            return ErrorResults.From(result.Error!);

        return Ok(result.Value);
    }

    [HttpPatch("{topicId:int}/task")]
    public async Task<IActionResult> Patch(int topicId, [FromBody] PatchTaskRequest request)
    {
        var current = ClaimsHelper.GetAuthenticatedUser(User);
        var result = await taskService.UpdateTaskAsync(
            current.Id, topicId, request.Description, request.Status);

        if (!result.Succeeded)
            return ErrorResults.From(result.Error!);

        return Ok(result.Value);
    }

    [HttpDelete("{topicId:int}/task")]
    public async Task<IActionResult> Delete(int topicId)
    {
        var current = ClaimsHelper.GetAuthenticatedUser(User);
        var result = await taskService.DeleteTaskAsync(current.Id, topicId);

        if (!result.Succeeded)
            return ErrorResults.From(result.Error!);

        return NoContent();
    }
}
