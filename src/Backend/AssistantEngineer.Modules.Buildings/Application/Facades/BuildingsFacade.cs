using AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Buildings.Application.Services.Buildings;
using AssistantEngineer.Modules.Buildings.Application.Services.Climate;
using AssistantEngineer.Modules.Buildings.Application.Services.Floors;
using AssistantEngineer.Modules.Buildings.Application.Services.Projects;
using AssistantEngineer.Modules.Buildings.Application.Services.Rooms;
using AssistantEngineer.Modules.Buildings.Application.Services.ThermalZones;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Buildings.Application.Facades;

public sealed class BuildingsFacade : IBuildingsFacade
{
    private readonly ProjectCommandService _projectCommand;
    private readonly ProjectQueryService _projectQuery;
    private readonly BuildingCommandService _buildingCommand;
    private readonly BuildingQueryService _buildingQuery;
    private readonly BuildingArchetypeService _buildingArchetypes;
    private readonly BuildingCalculationReadinessService _buildingReadiness;
    private readonly BuildingModelValidationService _buildingValidation;
    private readonly BuildingModelAutocorrectionService _buildingAutocorrection;
    private readonly FloorCommandService _floorCommand;
    private readonly FloorQueryService _floorQuery;
    private readonly RoomCommandService _roomCommand;
    private readonly RoomQueryService _roomQuery;
    private readonly RoomVentilationCommandService _roomVentilationCommand;
    private readonly RoomVentilationQueryService _roomVentilationQuery;
    private readonly RoomVentilationDefaultsService _roomVentilationDefaults;
    private readonly ThermalZoneCommandService _thermalZoneCommand;
    private readonly ThermalZoneQueryService _thermalZoneQuery;
    private readonly EpwAnnualClimateDataImportService _epwImport;
    private readonly PvgisAnnualClimateDataImportService _pvgisImport;
    private readonly RoomGroundContactService _roomGroundContact;

    public BuildingsFacade(
        ProjectCommandService projectCommand,
        ProjectQueryService projectQuery,
        BuildingCommandService buildingCommand,
        BuildingQueryService buildingQuery,
        BuildingArchetypeService buildingArchetypes,
        BuildingCalculationReadinessService buildingReadiness,
        BuildingModelValidationService buildingValidation,
        BuildingModelAutocorrectionService buildingAutocorrection,
        FloorCommandService floorCommand,
        FloorQueryService floorQuery,
        RoomCommandService roomCommand,
        RoomQueryService roomQuery,
        RoomVentilationCommandService roomVentilationCommand,
        RoomVentilationQueryService roomVentilationQuery,
        RoomVentilationDefaultsService roomVentilationDefaults,
        ThermalZoneCommandService thermalZoneCommand,
        ThermalZoneQueryService thermalZoneQuery,
        EpwAnnualClimateDataImportService epwImport,
        PvgisAnnualClimateDataImportService pvgisImport,
        RoomGroundContactService roomGroundContact)
    {
        _projectCommand = projectCommand;
        _projectQuery = projectQuery;
        _buildingCommand = buildingCommand;
        _buildingQuery = buildingQuery;
        _buildingArchetypes = buildingArchetypes;
        _buildingReadiness = buildingReadiness;
        _buildingValidation = buildingValidation;
        _buildingAutocorrection = buildingAutocorrection;
        _floorCommand = floorCommand;
        _floorQuery = floorQuery;
        _roomCommand = roomCommand;
        _roomQuery = roomQuery;
        _roomVentilationCommand = roomVentilationCommand;
        _roomVentilationQuery = roomVentilationQuery;
        _roomVentilationDefaults = roomVentilationDefaults;
        _thermalZoneCommand = thermalZoneCommand;
        _thermalZoneQuery = thermalZoneQuery;
        _epwImport = epwImport;
        _pvgisImport = pvgisImport;
        _roomGroundContact = roomGroundContact;
    }

    public Task<Result<ProjectResponse>> CreateProjectAsync(
        CreateProjectRequest request,
        CancellationToken cancellationToken) =>
        _projectCommand.CreateAsync(request, cancellationToken);

    public Task<Result<List<ProjectResponse>>> GetProjectsAsync(CancellationToken cancellationToken) =>
        _projectQuery.GetAllAsync(cancellationToken);

    public Task<Result<ProjectResponse>> GetProjectByIdAsync(
        int id,
        CancellationToken cancellationToken) =>
        _projectQuery.GetByIdAsync(id, cancellationToken);

    public Task<Result<ProjectResponse>> UpdateProjectAsync(
        int id,
        UpdateProjectRequest request,
        CancellationToken cancellationToken) =>
        _projectCommand.UpdateAsync(id, request, cancellationToken);

    public Task<Result> DeleteProjectAsync(
        int id,
        CancellationToken cancellationToken) =>
        _projectCommand.DeleteAsync(id, cancellationToken);

    public Task<Result<BuildingResponse>> CreateBuildingAsync(
        int projectId,
        CreateBuildingRequest request,
        CancellationToken cancellationToken) =>
        _buildingCommand.CreateAsync(projectId, request, cancellationToken);

    public Task<Result<BuildingResponse>> CreateBuildingFromArchetypeAsync(
        int projectId,
        CreateBuildingFromArchetypeRequest request,
        CancellationToken cancellationToken) =>
        _buildingArchetypes.CreateFromArchetypeAsync(projectId, request, cancellationToken);

    public Task<Result<BuildingResponse>> GetBuildingByIdAsync(
        int id,
        CancellationToken cancellationToken) =>
        _buildingQuery.GetByIdAsync(id, cancellationToken);

    public Task<Result<BuildingResponse>> UpdateBuildingAsync(
        int id,
        UpdateBuildingRequest request,
        CancellationToken cancellationToken) =>
        _buildingCommand.UpdateAsync(id, request, cancellationToken);

    public Task<Result> DeleteBuildingAsync(
        int id,
        CancellationToken cancellationToken) =>
        _buildingCommand.DeleteAsync(id, cancellationToken);

    public Task<Result<List<BuildingResponse>>> GetBuildingsByProjectAsync(
        int projectId,
        CancellationToken cancellationToken) =>
        _buildingQuery.GetByProjectIdAsync(projectId, cancellationToken);

    public Task<Result<BuildingCalculationReadinessReport>> CheckBuildingReadinessAsync(
        int buildingId,
        int weatherYear,
        CancellationToken cancellationToken) =>
        _buildingReadiness.CheckAsync(buildingId, weatherYear, cancellationToken);

    public IReadOnlyList<BuildingArchetypeSummary> ListBuildingArchetypes() =>
        _buildingArchetypes.ListArchetypes();

    public Task<Result<FloorResponse>> CreateFloorAsync(
        int buildingId,
        CreateFloorRequest request,
        CancellationToken cancellationToken) =>
        _floorCommand.CreateAsync(buildingId, request, cancellationToken);

    public Task<Result<List<FloorResponse>>> GetFloorsByBuildingAsync(
        int buildingId,
        CancellationToken cancellationToken) =>
        _floorQuery.GetByBuildingIdAsync(buildingId, cancellationToken);

    public Task<Result<FloorResponse>> GetFloorByIdAsync(
        int id,
        CancellationToken cancellationToken) =>
        _floorQuery.GetByIdAsync(id, cancellationToken);

    public Task<Result<FloorResponse>> UpdateFloorAsync(
        int id,
        UpdateFloorRequest request,
        CancellationToken cancellationToken) =>
        _floorCommand.UpdateAsync(id, request, cancellationToken);

    public Task<Result> DeleteFloorAsync(
        int id,
        CancellationToken cancellationToken) =>
        _floorCommand.DeleteAsync(id, cancellationToken);

    public Task<Result<RoomResponse>> CreateRoomAsync(
        CreateRoomRequest request,
        CancellationToken cancellationToken) =>
        _roomCommand.CreateAsync(request, cancellationToken);

    public Task<Result<List<RoomResponse>>> GetRoomsAsync(CancellationToken cancellationToken) =>
        _roomQuery.GetAllAsync(cancellationToken);

    public Task<Result<List<RoomResponse>>> GetRoomsByBuildingAsync(
        int buildingId,
        CancellationToken cancellationToken) =>
        _roomQuery.GetByBuildingIdAsync(buildingId, cancellationToken);

    public Task<Result<RoomResponse>> GetRoomByIdAsync(
        int id,
        CancellationToken cancellationToken) =>
        _roomQuery.GetByIdAsync(id, cancellationToken);

    public Task<Result<RoomResponse>> UpdateRoomAsync(
        int id,
        UpdateRoomRequest request,
        CancellationToken cancellationToken) =>
        _roomCommand.UpdateAsync(id, request, cancellationToken);

    public Task<Result> DeleteRoomAsync(
        int id,
        CancellationToken cancellationToken) =>
        _roomCommand.DeleteAsync(id, cancellationToken);

    public Task<Result<WindowResponse>> AddWindowAsync(
        int roomId,
        CreateWindowRequest request,
        CancellationToken cancellationToken) =>
        _roomCommand.AddWindowAsync(roomId, request, cancellationToken);

    public Task<Result<WindowResponse>> UpdateWindowAsync(
        int roomId,
        int windowId,
        UpdateWindowRequest request,
        CancellationToken cancellationToken) =>
        _roomCommand.UpdateWindowAsync(roomId, windowId, request, cancellationToken);

    public Task<Result> DeleteWindowAsync(
        int roomId,
        int windowId,
        CancellationToken cancellationToken) =>
        _roomCommand.DeleteWindowAsync(roomId, windowId, cancellationToken);

    public Task<Result<WallResponse>> AddWallAsync(
        int roomId,
        CreateWallRequest request,
        CancellationToken cancellationToken) =>
        _roomCommand.AddWallAsync(roomId, request, cancellationToken);

    public Task<Result<WallResponse>> UpdateWallAsync(
        int roomId,
        int wallId,
        UpdateWallRequest request,
        CancellationToken cancellationToken) =>
        _roomCommand.UpdateWallAsync(roomId, wallId, request, cancellationToken);

    public Task<Result> DeleteWallAsync(
        int roomId,
        int wallId,
        CancellationToken cancellationToken) =>
        _roomCommand.DeleteWallAsync(roomId, wallId, cancellationToken);

    public Task<Result<List<WindowResponse>>> GetRoomWindowsAsync(
        int roomId,
        CancellationToken cancellationToken) =>
        _roomQuery.GetWindowsAsync(roomId, cancellationToken);

    public Task<Result<List<WallResponse>>> GetRoomWallsAsync(
        int roomId,
        CancellationToken cancellationToken) =>
        _roomQuery.GetWallsAsync(roomId, cancellationToken);

    public Task<Result<RoomVentilationParametersResponse>> GetRoomVentilationParametersAsync(
        int roomId,
        CancellationToken cancellationToken) =>
        _roomVentilationQuery.GetAsync(roomId, cancellationToken);

    public Task<Result<RoomVentilationParametersResponse>> UpsertRoomVentilationParametersAsync(
        int roomId,
        UpsertRoomVentilationParametersRequest request,
        CancellationToken cancellationToken) =>
        _roomVentilationCommand.UpsertAsync(roomId, request, cancellationToken);

    public Task<Result> DeleteRoomVentilationParametersAsync(
        int roomId,
        CancellationToken cancellationToken) =>
        _roomVentilationCommand.DeleteAsync(roomId, cancellationToken);

    public Task<Result<RoomVentilationDefaultsResponse>> PreviewRoomVentilationDefaultsAsync(
        int roomId,
        CancellationToken cancellationToken) =>
        _roomVentilationDefaults.PreviewAsync(roomId, cancellationToken);

    public Task<Result<RoomVentilationParametersResponse>> ApplyRoomVentilationDefaultsAsync(
        int roomId,
        ApplyRoomVentilationDefaultsRequest request,
        CancellationToken cancellationToken) =>
        _roomVentilationDefaults.ApplyAsync(roomId, request, cancellationToken);

    public Task<Result<List<ThermalZoneResponse>>> GetThermalZonesByBuildingAsync(
        int buildingId,
        CancellationToken cancellationToken) =>
        _thermalZoneQuery.GetByBuildingIdAsync(buildingId, cancellationToken);

    public Task<Result<ThermalZoneResponse>> CreateThermalZoneAsync(
        int buildingId,
        CreateThermalZoneRequest request,
        CancellationToken cancellationToken) =>
        _thermalZoneCommand.CreateAsync(buildingId, request, cancellationToken);

    public Task<Result<ThermalZoneResponse>> UpdateThermalZoneAsync(
        int thermalZoneId,
        UpdateThermalZoneRequest request,
        CancellationToken cancellationToken) =>
        _thermalZoneCommand.UpdateAsync(thermalZoneId, request, cancellationToken);

    public Task<Result<ThermalZoneResponse>> GetThermalZoneByIdAsync(
        int thermalZoneId,
        CancellationToken cancellationToken) =>
        _thermalZoneQuery.GetByIdAsync(thermalZoneId, cancellationToken);

    public Task<Result> DeleteThermalZoneAsync(
        int thermalZoneId,
        CancellationToken cancellationToken) =>
        _thermalZoneCommand.DeleteAsync(thermalZoneId, cancellationToken);

    public Task<Result<AnnualClimateDataImportResponse>> ImportAnnualClimateDataFromEpwAsync(
        int climateZoneId,
        int year,
        Stream sourceFile,
        string sourceFileName,
        CancellationToken cancellationToken) =>
        _epwImport.ImportAsync(climateZoneId, year, sourceFile, sourceFileName, cancellationToken);

    public Task<Result<AnnualClimateDataImportResponse>> ImportAnnualClimateDataFromPvgisAsync(
        int climateZoneId,
        ImportPvgisWeatherRequest request,
        CancellationToken cancellationToken) =>
        _pvgisImport.ImportAsync(climateZoneId, request, cancellationToken);

    public Task<Result<BuildingValidationReport>> ValidateBuildingModelAsync(
        int buildingId,
        int weatherYear,
        CancellationToken cancellationToken) =>
        _buildingValidation.ValidateAsync(buildingId, weatherYear, cancellationToken);

    public Task<Result<BuildingAutocorrectionPreview>> PreviewBuildingAutocorrectionsAsync(
        int buildingId,
        int weatherYear,
        AutocorrectBuildingModelRequest request,
        CancellationToken cancellationToken) =>
        _buildingAutocorrection.PreviewAsync(buildingId, weatherYear, request, cancellationToken);

    public Task<Result<BuildingAutocorrectionResult>> ApplyBuildingAutocorrectionsAsync(
        int buildingId,
        int weatherYear,
        AutocorrectBuildingModelRequest request,
        CancellationToken cancellationToken) =>
        _buildingAutocorrection.ApplyAsync(buildingId, weatherYear, request, cancellationToken);
    
    public Task<Result<RoomGroundContactResponse>> GetRoomGroundContactAsync(
        int roomId,
        CancellationToken cancellationToken) =>
        _roomGroundContact.GetAsync(roomId, cancellationToken);

    public Task<Result<RoomGroundContactResponse>> UpsertRoomGroundContactAsync(
        int roomId,
        UpsertRoomGroundContactRequest request,
        CancellationToken cancellationToken) =>
        _roomGroundContact.UpsertAsync(roomId, request, cancellationToken);

    public Task<Result> DeleteRoomGroundContactAsync(
        int roomId,
        CancellationToken cancellationToken) =>
        _roomGroundContact.DeleteAsync(roomId, cancellationToken);
}
