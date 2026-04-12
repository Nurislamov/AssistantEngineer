using System.ComponentModel.DataAnnotations;

namespace AssistantEngineer.Contracts;

public class CreateProjectRequest
{
    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;
}