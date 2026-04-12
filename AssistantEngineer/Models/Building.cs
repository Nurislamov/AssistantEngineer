namespace AssistantEngineer.Models;

public class Building
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public List<Floor> Floors { get; set; } = [];
}