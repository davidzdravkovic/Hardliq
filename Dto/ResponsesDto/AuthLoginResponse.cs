namespace TaskManager.Dto.ResponsesDto;

public class AuthLoginResponse
{
    public required string Token { get; init; }
    public int Id { get; init; }
    public required string Username { get; init; }
    public required string Email { get; init; }
}
