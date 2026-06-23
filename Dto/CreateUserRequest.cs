using System.ComponentModel.DataAnnotations;

namespace TaskManager.Dto;

public class CreateUserRequest
{
    [Required(ErrorMessage = "Please enter a username.")]
    [StringLength(
        30,
        MinimumLength = 3,
        ErrorMessage = "Username must be between 3 and 30 characters.")]
    [RegularExpression(
        @"^[a-zA-Z0-9][a-zA-Z0-9_-]{2,29}$",
        ErrorMessage = "Username can contain letters, numbers, underscores, and hyphens and must start with a letter or number.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter an email address.")]
    [EmailAddress(
        ErrorMessage = "Please enter a valid email address (for example: name@example.com).")]
    [StringLength(
        254,
        ErrorMessage = "Email address must not exceed 254 characters.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter a password.")]
    [StringLength(
        128,
        MinimumLength = 8,
        ErrorMessage = "Password must be between 8 and 128 characters.")]
    public string Password { get; set; } = string.Empty;
}
