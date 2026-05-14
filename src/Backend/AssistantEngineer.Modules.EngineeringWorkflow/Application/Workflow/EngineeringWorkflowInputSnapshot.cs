using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Calculations.Application.Contracts.CoreStatus;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Api.Services.Calculations.Workflow;

public sealed record EngineeringWorkflowInputSnapshot(
    int ProjectId,
    int? RequestedBuildingId,
    int? SelectedBuildingId,
    int WeatherYear,
    Result<ProjectResponse> ProjectResult,
    Result<List<BuildingResponse>> BuildingsResult,
    Result<BuildingResponse>? BuildingResult,
    Result<List<RoomResponse>>? RoomsResult,
    Result<List<ThermalZoneResponse>>? ZonesResult,
    Result<BuildingCalculationReadinessReport>? ReadinessResult,
    Result<BuildingValidationReport>? ValidationResult,
    Result<EngineeringCoreV1StatusResponse> CoreStatusResult,
    IReadOnlyList<WallResponse> Walls,
    IReadOnlyList<WindowResponse> Windows,
    int VentilationConfiguredRoomCount,
    int GroundConfiguredRoomCount);