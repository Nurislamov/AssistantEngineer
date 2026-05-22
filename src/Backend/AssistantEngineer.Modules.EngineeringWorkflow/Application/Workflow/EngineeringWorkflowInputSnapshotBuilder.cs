using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Buildings.Application.Facades;
using AssistantEngineer.Modules.Calculations.Application.Facades;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.EngineeringWorkflow.Application.Workflow;

public sealed class EngineeringWorkflowInputSnapshotBuilder : IEngineeringWorkflowInputSnapshotBuilder
{
    private readonly IBuildingsFacade _buildings;
    private readonly IEngineeringCoreStatusFacade _engineeringCoreStatus;

    public EngineeringWorkflowInputSnapshotBuilder(
        IBuildingsFacade buildings,
        IEngineeringCoreStatusFacade engineeringCoreStatus)
    {
        _buildings = buildings;
        _engineeringCoreStatus = engineeringCoreStatus;
    }

    public async Task<EngineeringWorkflowInputSnapshot> BuildAsync(
        int projectId,
        int? buildingId,
        int weatherYear,
        CancellationToken cancellationToken)
    {
        var projectResult = await _buildings.GetProjectByIdAsync(projectId, cancellationToken);
        var buildingsResult = await _buildings.GetBuildingsByProjectAsync(projectId, cancellationToken);
        var coreStatusResult = _engineeringCoreStatus.GetEngineeringCoreV1Status();
        var selectedBuildingId = buildingId ?? (buildingsResult.IsSuccess ? buildingsResult.Value.FirstOrDefault()?.Id : null);

        if (!selectedBuildingId.HasValue)
        {
            return new EngineeringWorkflowInputSnapshot(
                ProjectId: projectId,
                RequestedBuildingId: buildingId,
                SelectedBuildingId: null,
                WeatherYear: weatherYear,
                ProjectResult: projectResult,
                BuildingsResult: buildingsResult,
                BuildingResult: null,
                RoomsResult: null,
                ZonesResult: null,
                ReadinessResult: null,
                ValidationResult: null,
                CoreStatusResult: coreStatusResult,
                Walls: [],
                Windows: [],
                VentilationConfiguredRoomCount: 0,
                GroundConfiguredRoomCount: 0);
        }

        var buildingResult = await _buildings.GetBuildingByIdAsync(selectedBuildingId.Value, cancellationToken);
        if (buildingResult.IsFailure)
        {
            return new EngineeringWorkflowInputSnapshot(
                ProjectId: projectId,
                RequestedBuildingId: buildingId,
                SelectedBuildingId: selectedBuildingId,
                WeatherYear: weatherYear,
                ProjectResult: projectResult,
                BuildingsResult: buildingsResult,
                BuildingResult: buildingResult,
                RoomsResult: null,
                ZonesResult: null,
                ReadinessResult: null,
                ValidationResult: null,
                CoreStatusResult: coreStatusResult,
                Walls: [],
                Windows: [],
                VentilationConfiguredRoomCount: 0,
                GroundConfiguredRoomCount: 0);
        }

        var roomsResult = await _buildings.GetRoomsByBuildingAsync(selectedBuildingId.Value, cancellationToken);
        var zonesResult = await _buildings.GetThermalZonesByBuildingAsync(selectedBuildingId.Value, cancellationToken);
        var readinessResult = await _buildings.CheckBuildingReadinessAsync(
            selectedBuildingId.Value,
            weatherYear,
            cancellationToken);
        var validationResult = await _buildings.ValidateBuildingModelAsync(
            selectedBuildingId.Value,
            weatherYear,
            cancellationToken);

        EngineeringWorkflowBulkInputResponse? bulkInput = null;
        if (roomsResult.IsSuccess)
        {
            var bulkInputResult = await _buildings.GetEngineeringWorkflowBulkInputAsync(
                selectedBuildingId.Value,
                cancellationToken);

            if (bulkInputResult.IsSuccess)
            {
                bulkInput = bulkInputResult.Value;
            }
        }

        var walls = new List<WallResponse>();
        var windows = new List<WindowResponse>();
        var ventilationConfiguredRoomCount = 0;
        var groundConfiguredRoomCount = 0;

        if (bulkInput is not null)
        {
            walls.AddRange(bulkInput.Walls);
            windows.AddRange(bulkInput.Windows);
            ventilationConfiguredRoomCount = bulkInput.VentilationConfiguredRoomCount;
            groundConfiguredRoomCount = bulkInput.GroundConfiguredRoomCount;
        }

        return new EngineeringWorkflowInputSnapshot(
            ProjectId: projectId,
            RequestedBuildingId: buildingId,
            SelectedBuildingId: selectedBuildingId,
            WeatherYear: weatherYear,
            ProjectResult: projectResult,
            BuildingsResult: buildingsResult,
            BuildingResult: buildingResult,
            RoomsResult: roomsResult,
            ZonesResult: zonesResult,
            ReadinessResult: readinessResult,
            ValidationResult: validationResult,
            CoreStatusResult: coreStatusResult,
            Walls: walls,
            Windows: windows,
            VentilationConfiguredRoomCount: ventilationConfiguredRoomCount,
            GroundConfiguredRoomCount: groundConfiguredRoomCount);
    }
}
