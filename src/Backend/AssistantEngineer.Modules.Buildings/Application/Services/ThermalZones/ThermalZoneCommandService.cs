using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Buildings.Application.Mappers;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.ThermalZones;
using AssistantEngineer.SharedKernel.Abstractions;
using AssistantEngineer.SharedKernel.Primitives;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AssistantEngineer.Modules.Buildings.Application.Services.ThermalZones;

public sealed class ThermalZoneCommandService
{
    private readonly IBuildingRepository _buildings;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ThermalZoneCommandService> _logger;

    public ThermalZoneCommandService(
        IBuildingRepository buildings,
        IUnitOfWork unitOfWork,
        ILogger<ThermalZoneCommandService>? logger = null)
    {
        _buildings = buildings;
        _unitOfWork = unitOfWork;
        _logger = logger ?? NullLogger<ThermalZoneCommandService>.Instance;
    }

    public async Task<Result<ThermalZoneResponse>> CreateAsync(
        int buildingId,
        CreateThermalZoneRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Creating thermal zone {ThermalZoneName} for building {BuildingId}.",
            request.Name,
            buildingId);

        var building = await _buildings.GetWithThermalZonesAndRoomsAsync(buildingId, cancellationToken);
        if (building is null)
            return Result<ThermalZoneResponse>.NotFound($"Building with id {buildingId} not found.");

        var requestedRoomsResult = ResolveRequestedRooms(building, request.RoomIds, buildingId);
        if (requestedRoomsResult.IsFailure)
            return Result<ThermalZoneResponse>.Failure(requestedRoomsResult);

        var conflicts = ValidateZoneConflicts(building, request.Name, requestedRoomsResult.Value, currentZoneId: null);
        if (conflicts.IsFailure)
            return Result<ThermalZoneResponse>.Failure(conflicts);

        var addResult = building.AddThermalZone(request.Name, requestedRoomsResult.Value);
        if (addResult.IsFailure)
            return Result<ThermalZoneResponse>.Failure(addResult);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created thermal zone {ThermalZoneId} for building {BuildingId}.",
            addResult.Value.Id,
            buildingId);

        return Result<ThermalZoneResponse>.Success(BuildingsMapper.ToResponse(addResult.Value));
    }

    public async Task<Result<ThermalZoneResponse>> UpdateAsync(
        int thermalZoneId,
        UpdateThermalZoneRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating thermal zone {ThermalZoneId}.", thermalZoneId);

        var building = await _buildings.GetByThermalZoneIdAsync(thermalZoneId, cancellationToken);
        if (building is null)
            return Result<ThermalZoneResponse>.NotFound($"Thermal zone with id {thermalZoneId} not found.");

        var thermalZone = building.ThermalZones.FirstOrDefault(zone => zone.Id == thermalZoneId);
        if (thermalZone is null)
            return Result<ThermalZoneResponse>.NotFound($"Thermal zone with id {thermalZoneId} not found.");

        var requestedRoomsResult = ResolveRequestedRooms(building, request.RoomIds, building.Id);
        if (requestedRoomsResult.IsFailure)
            return Result<ThermalZoneResponse>.Failure(requestedRoomsResult);

        var conflicts = ValidateZoneConflicts(building, request.Name, requestedRoomsResult.Value, thermalZoneId);
        if (conflicts.IsFailure)
            return Result<ThermalZoneResponse>.Failure(conflicts);

        var renameResult = thermalZone.Rename(request.Name);
        if (renameResult.IsFailure)
            return Result<ThermalZoneResponse>.Failure(renameResult);

        var replaceRoomsResult = thermalZone.ReplaceRooms(requestedRoomsResult.Value);
        if (replaceRoomsResult.IsFailure)
            return Result<ThermalZoneResponse>.Failure(replaceRoomsResult);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated thermal zone {ThermalZoneId}.", thermalZoneId);
        return Result<ThermalZoneResponse>.Success(BuildingsMapper.ToResponse(thermalZone));
    }

    public async Task<Result> DeleteAsync(
        int thermalZoneId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting thermal zone {ThermalZoneId}.", thermalZoneId);

        var building = await _buildings.GetByThermalZoneIdAsync(thermalZoneId, cancellationToken);
        if (building is null)
            return Result.NotFound($"Thermal zone with id {thermalZoneId} not found.");

        var removeResult = building.RemoveThermalZone(thermalZoneId);
        if (removeResult.IsFailure)
            return removeResult;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted thermal zone {ThermalZoneId}.", thermalZoneId);
        return Result.Success();
    }

    private static Result<List<Room>> ResolveRequestedRooms(
        Building building,
        IEnumerable<int> requestedRoomIds,
        int buildingId)
    {
        var buildingRooms = building.Floors
            .SelectMany(floor => floor.Rooms)
            .ToDictionary(room => room.Id);

        var resolvedRooms = new List<Room>();

        foreach (var roomId in requestedRoomIds.Distinct())
        {
            if (!buildingRooms.TryGetValue(roomId, out var room))
            {
                return Result<List<Room>>.Validation(
                    $"Room {roomId} does not belong to building {buildingId}.");
            }

            resolvedRooms.Add(room);
        }

        return Result<List<Room>>.Success(resolvedRooms);
    }

    private static Result ValidateZoneConflicts(
        Building building,
        string requestedName,
        IReadOnlyCollection<Room> requestedRooms,
        int? currentZoneId)
    {
        var duplicateNameZone = building.ThermalZones.FirstOrDefault(zone =>
            zone.Id != currentZoneId &&
            zone.Name.Equals(requestedName, StringComparison.OrdinalIgnoreCase));

        if (duplicateNameZone is not null)
        {
            return Result.Conflict(
                $"Thermal zone with name '{requestedName}' already exists in this building.");
        }

        var conflictingAssignment = building.ThermalZones
            .Where(zone => zone.Id != currentZoneId)
            .SelectMany(zone => zone.AssignedRooms.Select(room => new { Zone = zone, Room = room }))
            .FirstOrDefault(item => requestedRooms.Any(requestedRoom => requestedRoom.Id == item.Room.Id));

        if (conflictingAssignment is not null)
        {
            return Result.Conflict(
                $"Room {conflictingAssignment.Room.Id} is already assigned to thermal zone '{conflictingAssignment.Zone.Name}'.");
        }

        return Result.Success();
    }
}