using System.ComponentModel.DataAnnotations;
using TaskManager.Models;

namespace TaskManager.Dto;

public class PutTaskRequest
{
    [Required(ErrorMessage = "Please enter a description.")]
    [StringLength(2000, MinimumLength = 1, ErrorMessage = "Description must be between 1 and 2000 characters.")]
    public string Description { get; set; } = string.Empty;

    public TaskItemStatus Status { get; set; } = TaskItemStatus.Pending;
}
