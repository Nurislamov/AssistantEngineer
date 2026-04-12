using System.ComponentModel.DataAnnotations;

namespace AssistantEngineer.Contracts;

public class CreateWallRequest
{
    [Range(0.1, 1000)]
    public double AreaM2 { get; set; }

    public bool IsExternal { get; set; }
}