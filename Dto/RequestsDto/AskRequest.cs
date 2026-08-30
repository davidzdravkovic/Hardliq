using System.ComponentModel.DataAnnotations;

namespace TaskManager.Dto.RequestsDto;

public class AskRequest
{
    [Required(ErrorMessage = "Please enter a question.")]
    [StringLength(500, MinimumLength = 2, ErrorMessage = "Question must be between 2 and 500 characters.")]
    public string Question { get; set; } = string.Empty;
}
