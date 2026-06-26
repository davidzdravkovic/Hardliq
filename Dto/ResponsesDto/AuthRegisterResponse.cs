namespace TaskManager.Dto.ResponsesDto;

public class AuthRegisterResponse
{
    public int Id { get; init; }
    public required string Username { get; init; }
    public required string Email { get; init; }
    public required string Token { get; init; }
}
