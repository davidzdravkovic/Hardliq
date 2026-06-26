using System.ComponentModel.DataAnnotations;
using TaskManager.Models;

namespace TaskManager.Dto.RequestsDto;

public class CreateTaskRequest
{
    [Required(ErrorMessage = "Please enter a name.")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 100 characters.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter a description.")]
    [StringLength(2000, MinimumLength = 1, ErrorMessage = "Description must be between 1 and 2000 characters.")]
    public string Description { get; set; } = string.Empty;

    public TaskItemStatus Status { get; set; } = TaskItemStatus.Pending;
}
