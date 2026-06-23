using System.ComponentModel.DataAnnotations;

namespace TaskManager.Dto;

public class CreateTopicRequest
{
    [Required(ErrorMessage = "Please enter a name.")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 100 characters.")]
    public string Name { get; set; } = string.Empty;

    public int? ParentId { get; set; }
}
