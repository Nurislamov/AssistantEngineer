namespace AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;

public sealed class EngineeringWorkflowBulkInputResponse
{
    public IReadOnlyList<EngineeringWorkflowRoomInputResponse> Rooms { get; set; } = [];
    public IReadOnlyList<WallResponse> Walls { get; set; } = [];
    public IReadOnlyList<WindowResponse> Windows { get; set; } = [];
    public int VentilationConfiguredRoomCount { get; set; }
    public int GroundConfiguredRoomCount { get; set; }
}
