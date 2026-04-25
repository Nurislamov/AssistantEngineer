using AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Buildings.Application.Facades;

public interface IBuildingsFacade
{
    Task<Result<ProjectResponse>> CreateProjectAsync(
        CreateProjectRequest request,
        CancellationToken cancellationToken);

    Task<Result<List<ProjectResponse>>> GetProjectsAsync(CancellationToken cancellationToken);

    Task<Result<ProjectResponse>> GetProjectByIdAsync(
        int id,
        CancellationToken cancellationToken);

    Task<Result<BuildingResponse>> CreateBuildingAsync(
        int projectId,
        CreateBuildingRequest request,
        CancellationToken cancellationToken);

    Task<Result<BuildingResponse>> CreateBuildingFromArchetypeAsync(
        int projectId,
        CreateBuildingFromArchetypeRequest request,
        CancellationToken cancellationToken);

    Task<Result<BuildingResponse>> GetBuildingByIdAsync(
        int id,
        CancellationToken cancellationToken);

    Task<Result<List<BuildingResponse>>> GetBuildingsByProjectAsync(
        int projectId,
        CancellationToken cancellationToken);

    Task<Result<BuildingCalculationReadinessReport>> CheckBuildingReadinessAsync(
        int buildingId,
        int weatherYear,
        CancellationToken cancellationToken);

    IReadOnlyList<BuildingArchetypeSummary> ListBuildingArchetypes();

    Task<Result<FloorResponse>> CreateFloorAsync(
        int buildingId,
        CreateFloorRequest request,
        CancellationToken cancellationToken);

    Task<Result<List<FloorResponse>>> GetFloorsByBuildingAsync(
        int buildingId,
        CancellationToken cancellationToken);

    Task<Result<FloorResponse>> GetFloorByIdAsync(
        int id,
        CancellationToken cancellationToken);

    Task<Result<RoomResponse>> CreateRoomAsync(
        CreateRoomRequest request,
        CancellationToken cancellationToken);

    Task<Result<List<RoomResponse>>> GetRoomsAsync(CancellationToken cancellationToken);

    Task<Result<RoomResponse>> GetRoomByIdAsync(
        int id,
        CancellationToken cancellationToken);

    Task<Result<WindowResponse>> AddWindowAsync(
        int roomId,
        CreateWindowRequest request,
        CancellationToken cancellationToken);

    Task<Result<WallResponse>> AddWallAsync(
        int roomId,
        CreateWallRequest request,
        CancellationToken cancellationToken);

    Task<Result<List<WindowResponse>>> GetRoomWindowsAsync(
        int roomId,
        CancellationToken cancellationToken);

    Task<Result<List<WallResponse>>> GetRoomWallsAsync(
        int roomId,
        CancellationToken cancellationToken);

    Task<Result<RoomVentilationParametersResponse>> GetRoomVentilationParametersAsync(
        int roomId,
        CancellationToken cancellationToken);

    Task<Result<RoomVentilationParametersResponse>> UpsertRoomVentilationParametersAsync(
        int roomId,
        UpsertRoomVentilationParametersRequest request,
        CancellationToken cancellationToken);

    Task<Result> DeleteRoomVentilationParametersAsync(
        int roomId,
        CancellationToken cancellationToken);

    Task<Result<List<ThermalZoneResponse>>> GetThermalZonesByBuildingAsync(
        int buildingId,
        CancellationToken cancellationToken);

    Task<Result<ThermalZoneResponse>> CreateThermalZoneAsync(
        int buildingId,
        CreateThermalZoneRequest request,
        CancellationToken cancellationToken);

    Task<Result<ThermalZoneResponse>> UpdateThermalZoneAsync(
        int thermalZoneId,
        UpdateThermalZoneRequest request,
        CancellationToken cancellationToken);

    Task<Result<ThermalZoneResponse>> GetThermalZoneByIdAsync(
        int thermalZoneId,
        CancellationToken cancellationToken);

    Task<Result> DeleteThermalZoneAsync(
        int thermalZoneId,
        CancellationToken cancellationToken);

    Task<Result<AnnualClimateDataImportResponse>> ImportAnnualClimateDataFromEpwAsync(
        int climateZoneId,
        int year,
        Stream sourceFile,
        string sourceFileName,
        CancellationToken cancellationToken);

    Task<Result<AnnualClimateDataImportResponse>> ImportAnnualClimateDataFromPvgisAsync(
        int climateZoneId,
        ImportPvgisWeatherRequest request,
        CancellationToken cancellationToken);
    
    Task<Result<BuildingValidationReport>> ValidateBuildingModelAsync(
        int buildingId,
        int weatherYear,
        CancellationToken cancellationToken);

    Task<Result<BuildingAutocorrectionPreview>> PreviewBuildingAutocorrectionsAsync(
        int buildingId,
        int weatherYear,
        AutocorrectBuildingModelRequest request,
        CancellationToken cancellationToken);

    Task<Result<BuildingAutocorrectionResult>> ApplyBuildingAutocorrectionsAsync(
        int buildingId,
        int weatherYear,
        AutocorrectBuildingModelRequest request,
        CancellationToken cancellationToken);
}
