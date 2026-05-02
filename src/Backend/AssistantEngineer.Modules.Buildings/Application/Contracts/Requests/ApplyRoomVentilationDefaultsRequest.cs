namespace AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;

public sealed class ApplyRoomVentilationDefaultsRequest
{
    public bool OverwriteExistingParameters { get; set; } = false;
}