using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TaskManager.Dto.RequestsDto;

public class PatchTopicRequest
{
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 100 characters.")]
    public string? Name { get; set; }

    public bool MoveParent { get; set; }

    public int? ParentId { get; set; }

    [RegularExpression("^(up|down)$", ErrorMessage = "Move must be up or down.")]
    public string? Move { get; set; }

    [JsonIgnore]
    public bool HasChanges =>
        Name is not null || MoveParent || Move is not null;
}
