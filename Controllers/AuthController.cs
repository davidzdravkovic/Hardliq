using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TaskManager.Auth;
using TaskManager.Data;
using TaskManager.Dto;
using TaskManager.Models;

namespace TaskManager.Controllers;

[ApiController]
[Route("auth")]
public class AuthController(AppDbContext db, JwtTokenService tokenService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] CreateUserRequest request)
    {
                    
          var conflict = await db.Users
                    .Where(u => u.Email == request.Email || u.Username == request.Username)
                    .Select(u => new { u.Email, u.Username })
                    .FirstOrDefaultAsync();

        if (conflict is not null)
        {
          if (conflict.Username == request.Username)
              return Conflict(new { message = "Username already taken." });
              return Conflict(new { message = "Email already taken." });
        }

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            Password = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };

        db.Users.Add(user);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg && pg.SqlState == "23505")
        {
            return Conflict(new { message = "Username or email already taken." });
        }

        var token = tokenService.CreateToken(user);

        return StatusCode(StatusCodes.Status201Created, new { user.Id, user.Username, user.Email, token });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == request.Username);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
            return Unauthorized();

        var token = tokenService.CreateToken(user);

        return Ok(new
        {
            token,
            user.Id,
            user.Username,
            user.Email
        });
    }
}
