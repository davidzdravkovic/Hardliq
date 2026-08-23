using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Dto.RequestsDto;
using TaskManager.Services;

namespace TaskManager.Controllers;

[ApiController]
[Route("auth")]
[AllowAnonymous]
public class AuthController(AuthService authService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] CreateUserRequest request)
    {
        var result = await authService.Register(request.Username, request.Email, request.Password);

        if (!result.Succeeded)
            return ErrorResults.From(result.Error!);

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await authService.Authenticate(request.Username, request.Password);

        if (!result.Succeeded)
            return ErrorResults.From(result.Error!);

        return Ok(result.Value);
    }
}
