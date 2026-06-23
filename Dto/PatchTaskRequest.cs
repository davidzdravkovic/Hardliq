using System.ComponentModel.DataAnnotations;
using TaskManager.Models;

namespace TaskManager.Dto;

public class PatchTaskRequest
{
    [StringLength(2000, MinimumLength = 1, ErrorMessage = "Description must be between 1 and 2000 characters.")]
    public string? Description { get; set; }

    public TaskItemStatus? Status { get; set; }
}
