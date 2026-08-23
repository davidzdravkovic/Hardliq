using Microsoft.EntityFrameworkCore;
using Npgsql;
using TaskManager.Auth;
using TaskManager.Data;
using TaskManager.Dto.ResponsesDto;
using TaskManager.Errors;
using TaskManager.Models;

namespace TaskManager.Services;

public class AuthService(AppDbContext db, JwtTokenService jwtTokenService)
{
    public async Task<Result<AuthLoginResponse>> Authenticate(string username, string password)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username);

        if (user is null || !BCrypt.Net.BCrypt.Verify(password, user.Password))
            return Result<AuthLoginResponse>.Fail(ErrorCodes.InvalidCredentials);

        return Result<AuthLoginResponse>.Success(new AuthLoginResponse
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Token = jwtTokenService.CreateToken(user)
        });
    }

    public async Task<Result<AuthRegisterResponse>> Register(string username, string email, string password)
    {
        var conflict = await db.Users
            .Where(u => u.Email == email || u.Username == username)
            .Select(u => new { u.Email, u.Username })
            .FirstOrDefaultAsync();

        if (conflict is not null)
        {
            if (conflict.Username == username)
            return Result<AuthRegisterResponse>.Fail(ErrorCodes.UsernameTaken);

            return Result<AuthRegisterResponse>.Fail(ErrorCodes.EmailTaken);
        }

        var user = new User
        {
            Username = username,
            Email = email,
            Password = BCrypt.Net.BCrypt.HashPassword(password)
        };

        db.Users.Add(user);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg && pg.SqlState == "23505")
        {
            var usernameTaken = await db.Users.AnyAsync(u => u.Username == username);
            return Result<AuthRegisterResponse>.Fail(usernameTaken ? ErrorCodes.UsernameTaken : ErrorCodes.EmailTaken);
        }

        return Result<AuthRegisterResponse>.Success(new AuthRegisterResponse
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Token = jwtTokenService.CreateToken(user)
        });
    }
}
