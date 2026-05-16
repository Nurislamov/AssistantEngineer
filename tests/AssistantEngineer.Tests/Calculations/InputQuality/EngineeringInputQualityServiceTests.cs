using System.Reflection;
using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Buildings.Domain.Climate;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Buildings.Domain.Settings;
using AssistantEngineer.Modules.Calculations.Application.Services.InputQuality;
using AssistantEngineer.Modules.Calculations.Application.Contracts.InputQuality;
using AssistantEngineer.SharedKernel.Diagnostics;
using AssistantEngineer.SharedKernel.Primitives;
using AssistantEngineer.SharedKernel.ValueObjects;
using Microsoft.Extensions.Logging;

namespace AssistantEngineer.Tests.Calculations.InputQuality;

public sealed class EngineeringInputQualityServiceTests
{
    [Fact]
    public async Task CheckRoomInputQualityAsync_ReturnsNotFoundForMissingRoom()
    {
        var service = CreateService(room: null);

        var result = await service.CheckRoomInputQualityAsync(roomId: 404);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.NotFound, result.ErrorType);
        Assert.Contains("Room with id 404 not found", result.Error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CheckRoomInputQualityAsync_NonPositiveAreaProducesIqRoom010()
    {
        var room = CreateRoomWithExternalEnvelope();
        SetInitOnlyDouble(room.Area, "<SquareMeters>k__BackingField", 0.0);
        var service = CreateService(room: room, preferences: CalculationPreferences.Default());

        var result = await service.CheckRoomInputQualityAsync(room.Id);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Contains(result.Value.Diagnostics, diagnostic =>
            diagnostic.Code == "IQ-ROOM-010" &&
            (diagnostic.Severity == EngineeringInputQualitySeverity.Error ||
             diagnostic.Severity == EngineeringInputQualitySeverity.Blocking));
    }

    [Fact]
    public async Task CheckRoomInputQualityAsync_SuspiciousWindowToFloorRatioProducesWarningWithoutBlockingReadiness()
    {
        var room = CreateRoomWithExternalEnvelope();
        var window = room.Windows.Single();
        SetInitOnlyDouble(window.Area, "<SquareMeters>k__BackingField", 9.0);
        var service = CreateService(room: room, preferences: CalculationPreferences.Default());

        var result = await service.CheckRoomInputQualityAsync(room.Id);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Contains(result.Value.Diagnostics, diagnostic =>
            diagnostic.Code == "IQ-ROOM-030" &&
            diagnostic.Severity == EngineeringInputQualitySeverity.Warning);
        Assert.False(result.Value.HasBlockingIssues);
        Assert.True(result.Value.IsCalculationReady);
    }

    [Fact]
    public async Task CheckRoomInputQualityAsync_MissingVentilationConfigurationProducesWarning()
    {
        var room = CreateRoomWithExternalEnvelope(includeVentilation: false);
        var service = CreateService(room: room, preferences: CalculationPreferences.Default());

        var result = await service.CheckRoomInputQualityAsync(room.Id);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Contains(result.Value.Diagnostics, diagnostic =>
            diagnostic.Code == "IQ-ROOM-040" &&
            diagnostic.Severity == EngineeringInputQualitySeverity.Warning);
    }

    [Fact]
    public async Task CheckRoomInputQualityAsync_InvalidShgcProducesIqRoom060()
    {
        var room = CreateRoomWithExternalEnvelope();
        var window = room.Windows.Single();
        SetInitOnlyDouble(window.Shgc, "<Value>k__BackingField", 1.2);
        var service = CreateService(room: room, preferences: CalculationPreferences.Default());

        var result = await service.CheckRoomInputQualityAsync(room.Id);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Contains(result.Value.Diagnostics, diagnostic =>
            diagnostic.Code == "IQ-ROOM-060" &&
            (diagnostic.Severity == EngineeringInputQualitySeverity.Error ||
             diagnostic.Severity == EngineeringInputQualitySeverity.Blocking));
    }

    [Fact]
    public async Task CheckBuildingInputQualityAsync_BuildingWithoutRoomsProducesIqBld011()
    {
        var building = CreateBuilding(includeFloor: true, includeRoom: false);
        var service = CreateService(building: building, preferences: CalculationPreferences.Default());

        var result = await service.CheckBuildingInputQualityAsync(building.Id);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Contains(result.Value.Diagnostics, diagnostic => diagnostic.Code == "IQ-BLD-011");
        Assert.True(result.Value.HasBlockingIssues);
        Assert.False(result.Value.IsCalculationReady);
    }

    [Fact]
    public async Task CheckBuildingInputQualityAsync_MissingClimateZoneProducesIqBld020Warning()
    {
        var building = CreateBuilding(includeFloor: true, includeRoom: true, withClimateZone: false);
        var service = CreateService(building: building, room: building.Floors.Single().Rooms.Single(), preferences: CalculationPreferences.Default());

        var result = await service.CheckBuildingInputQualityAsync(building.Id);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Contains(result.Value.Diagnostics, diagnostic =>
            diagnostic.Code == "IQ-BLD-020" &&
            diagnostic.Severity == EngineeringInputQualitySeverity.Warning);
    }

    [Fact]
    public async Task CheckBuildingInputQualityAsync_HighestSeverityAndBlockingFlagsAreComputed()
    {
        var building = CreateBuilding(includeFloor: false, includeRoom: false, withClimateZone: false);
        var service = CreateService(building: building, preferences: CalculationPreferences.Default());

        var result = await service.CheckBuildingInputQualityAsync(building.Id);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(EngineeringInputQualitySeverity.Blocking, result.Value.HighestSeverity);
        Assert.True(result.Value.HasBlockingIssues);
        Assert.False(result.Value.IsCalculationReady);
    }

    [Fact]
    public async Task CheckRoomInputQualityAsync_LogsStartAndCompletionEventCodes()
    {
        var room = CreateRoomWithExternalEnvelope(includeVentilation: true);
        var logger = new CapturingLogger<EngineeringInputQualityService>();
        var service = CreateService(room: room, preferences: CalculationPreferences.Default(), logger: logger);

        var result = await service.CheckRoomInputQualityAsync(room.Id);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Contains(logger.Entries, item => item.EventCode == ObservabilityEventCodes.InputQualityCheckStarted);
        Assert.Contains(logger.Entries, item => item.EventCode == ObservabilityEventCodes.InputQualityCheckCompleted);
    }

    [Fact]
    public async Task CheckBuildingInputQualityAsync_LogsBlockingIssueEventWhenBuildingNotReady()
    {
        var building = CreateBuilding(includeFloor: false, includeRoom: false, withClimateZone: false);
        var logger = new CapturingLogger<EngineeringInputQualityService>();
        var service = CreateService(building: building, preferences: CalculationPreferences.Default(), logger: logger);

        var result = await service.CheckBuildingInputQualityAsync(building.Id);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Contains(logger.Entries, item => item.EventCode == ObservabilityEventCodes.InputQualityBlockingIssueDetected);
    }

    private static EngineeringInputQualityService CreateService(
        Building? building = null,
        Room? room = null,
        CalculationPreferences? preferences = null,
        ILogger<EngineeringInputQualityService>? logger = null)
    {
        return new EngineeringInputQualityService(
            new BuildingRepositoryStub(building),
            new RoomRepositoryStub(room),
            new CalculationPreferencesRepositoryStub(preferences),
            logger);
    }

    private static Building CreateBuilding(
        bool includeFloor,
        bool includeRoom,
        bool withClimateZone = false)
    {
        var project = DomainInvariantTests.CreateProject("Input quality project");
        var building = DomainInvariantTests.CreateBuilding(project, "Input quality building");
        SetEntityId(project, 1001);
        SetEntityId(building, 2001);
        SetEntityBackingField(building, "<ProjectId>k__BackingField", 1001);

        if (withClimateZone)
        {
            var climate = ClimateZone.Create(
                "CZ-QA",
                Temperature.FromCelsius(36).Value,
                Temperature.FromCelsius(-8).Value).Value;
            SetEntityId(climate, 3001);
            Assert.True(building.SetClimateZone(climate).IsSuccess);
        }

        if (!includeFloor)
            return building;

        var floor = building.AddFloor("Level 1").Value;
        SetEntityId(floor, 4001);
        SetEntityBackingField(floor, "<BuildingId>k__BackingField", building.Id);

        if (!includeRoom)
            return building;

        var room = floor.AddRoom(
            name: "Room 1",
            area: Area.FromSquareMeters(10).Value,
            heightM: 3.0,
            indoorTemp: Temperature.FromCelsius(20).Value,
            outdoorTemperatureOverride: Temperature.FromCelsius(-5).Value,
            peopleCount: 2,
            equipmentLoad: Power.FromWatts(150).Value,
            lightingLoad: Power.FromWatts(120).Value,
            type: RoomType.Office).Value;

        SetEntityId(room, 5001);
        SetEntityBackingField(room, "<FloorId>k__BackingField", floor.Id);
        return building;
    }

    private static Room CreateRoomWithExternalEnvelope(bool includeVentilation = false)
    {
        var building = CreateBuilding(includeFloor: true, includeRoom: true, withClimateZone: true);
        var room = building.Floors.Single().Rooms.Single();

        Assert.True(room.AddWall(
            area: Area.FromSquareMeters(12).Value,
            uValue: ThermalTransmittance.FromValue(0.55).Value,
            orientation: CardinalDirection.South,
            boundaryType: WallBoundaryType.External).IsSuccess);

        Assert.True(room.AddWindow(
            area: Area.FromSquareMeters(3.0).Value,
            uValue: ThermalTransmittance.FromValue(1.4).Value,
            shgc: SolarHeatGainCoefficient.FromValue(0.55).Value,
            orientation: CardinalDirection.South).IsSuccess);

        if (includeVentilation)
        {
            var ventilation = AssistantEngineer.Modules.Buildings.Domain.Ventilation.VentilationParameters.Create(
                airChangesPerHour: 0.6,
                heatRecoveryEfficiency: 0.0).Value;
            Assert.True(room.SetVentilationParameters(ventilation).IsSuccess);
        }

        return room;
    }

    private static void SetEntityId(object entity, int id) =>
        SetEntityBackingField(entity, "<Id>k__BackingField", id);

    private static void SetEntityBackingField(object entity, string fieldName, int value)
    {
        var field = entity.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        field!.SetValue(entity, value);
    }

    private static void SetInitOnlyDouble(object target, string backingFieldName, double value)
    {
        var field = target.GetType().GetField(backingFieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        field!.SetValue(target, value);
    }

    private sealed class BuildingRepositoryStub : IBuildingRepository
    {
        private readonly Building? _building;

        public BuildingRepositoryStub(Building? building) => _building = building;

        public Task<Building?> GetByIdAsync(int id, bool includeClimateZone = false, CancellationToken cancellationToken = default) =>
            Task.FromResult<Building?>(_building is not null && id == _building.Id ? _building : null);

        public Task<Building?> GetWithFloorsAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Building?>(_building is not null && id == _building.Id ? _building : null);

        public Task<Building?> GetWithThermalZonesAndRoomsAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Building?>(_building is not null && id == _building.Id ? _building : null);

        public Task<Building?> GetByThermalZoneIdAsync(int thermalZoneId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Building?>(null);

        public Task<Building?> GetForCalculationAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Building?>(_building is not null && id == _building.Id ? _building : null);

        public Task<Building?> GetForReportAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Building?>(null);

        public Task<IReadOnlyList<Building>> ListByProjectIdAsync(int projectId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Building>>(_building is not null && _building.ProjectId == projectId ? [_building] : []);

        public void Add(Building building) => throw new NotSupportedException();
        public void Remove(Building building) => throw new NotSupportedException();

        public Task<Building?> GetForValidationAsync(int id, bool asTracking = false, CancellationToken cancellationToken = default) =>
            Task.FromResult<Building?>(_building is not null && id == _building.Id ? _building : null);
    }

    private sealed class RoomRepositoryStub : IRoomRepository
    {
        private readonly Room? _room;

        public RoomRepositoryStub(Room? room) => _room = room;

        public Task<Room?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Room?>(_room is not null && id == _room.Id ? _room : null);

        public Task<Room?> GetForCalculationAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Room?>(_room is not null && id == _room.Id ? _room : null);

        public Task<Room?> GetWithWindowsAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Room?>(_room is not null && id == _room.Id ? _room : null);

        public Task<Room?> GetWithWallsAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Room?>(_room is not null && id == _room.Id ? _room : null);

        public Task<Room?> GetWithWindowsAndWallsAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Room?>(_room is not null && id == _room.Id ? _room : null);

        public Task<Room?> GetWithVentilationAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Room?>(_room is not null && id == _room.Id ? _room : null);

        public Task<IReadOnlyList<Room>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Room>>(_room is null ? [] : [_room]);

        public Task<IReadOnlyList<Room>> ListByBuildingIdAsync(int buildingId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Room>>(_room is not null && _room.Floor.BuildingId == buildingId ? [_room] : []);

        public Task<IReadOnlyList<Room>> ListWithEngineeringInputsByBuildingIdAsync(int buildingId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Room>>(_room is not null && _room.Floor.BuildingId == buildingId ? [_room] : []);

        public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_room is not null && _room.Id == id);

        public Task<IReadOnlyList<Window>> ListWindowsAsync(int roomId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Window>>(_room is not null && _room.Id == roomId ? _room.Windows.ToArray() : []);

        public Task<IReadOnlyList<Wall>> ListWallsAsync(int roomId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Wall>>(_room is not null && _room.Id == roomId ? _room.Walls.ToArray() : []);

        public void Add(Room room) => throw new NotSupportedException();
        public void Remove(Room room) => throw new NotSupportedException();
        public void RemoveWindow(Window window) => throw new NotSupportedException();
        public void RemoveWall(Wall wall) => throw new NotSupportedException();
    }

    private sealed class CalculationPreferencesRepositoryStub : ICalculationPreferencesRepository
    {
        private readonly CalculationPreferences? _preferences;

        public CalculationPreferencesRepositoryStub(CalculationPreferences? preferences) => _preferences = preferences;

        public Task<CalculationPreferences?> GetByProjectIdAsync(int projectId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_preferences);
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);
            var eventCode = string.Empty;

            if (state is IEnumerable<KeyValuePair<string, object?>> properties)
            {
                foreach (var property in properties)
                {
                    if (string.Equals(property.Key, "EventCode", StringComparison.Ordinal))
                    {
                        eventCode = property.Value?.ToString() ?? string.Empty;
                        break;
                    }
                }
            }

            Entries.Add(new LogEntry(logLevel, eventCode, message));
        }
    }

    private sealed record LogEntry(LogLevel LogLevel, string EventCode, string Message);
}
