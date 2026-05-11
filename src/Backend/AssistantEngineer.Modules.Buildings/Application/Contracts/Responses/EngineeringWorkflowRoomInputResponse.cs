namespace AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;

public sealed class EngineeringWorkflowRoomInputResponse
{
    public int RoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public bool HasVentilationParameters { get; set; }
    public bool HasGroundContactMetadata { get; set; }
}
