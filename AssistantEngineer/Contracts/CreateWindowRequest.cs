using System.ComponentModel.DataAnnotations;

namespace AssistantEngineer.Contracts;

public class CreateWindowRequest
{
    [Range(0.1, 100)]
    public double AreaM2 { get; set; }
}