using System.ComponentModel.DataAnnotations;

namespace AssistantEngineer.Contracts;

public class CreateBuildingRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;
}