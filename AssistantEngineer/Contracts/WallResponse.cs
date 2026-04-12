namespace AssistantEngineer.Contracts;

public class WallResponse
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public double AreaM2 { get; set; }
    public bool IsExternal { get; set; }
}