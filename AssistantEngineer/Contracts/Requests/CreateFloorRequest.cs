using System.ComponentModel.DataAnnotations;

namespace AssistantEngineer.Contracts.Requests;

public class CreateFloorRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;
}