namespace AssistantEngineer.Modules.Buildings.Application.Contracts.Common;

public static class BuildingValidationCodes
{
    public const string MissingClimateZone = "building.climate-zone.missing";
    public const string MissingRooms = "building.rooms.missing";

    public const string RoomAreaNonPositive = "room.area.non-positive";
    public const string RoomHeightNonPositive = "room.height.non-positive";
    public const string RoomPeopleCountNegative = "room.people-count.negative";
    public const string RoomEquipmentLoadNegative = "room.equipment-load.negative";
    public const string RoomLightingLoadNegative = "room.lighting-load.negative";
    public const string RoomWindowsWithoutExternalWall = "room.windows.no-external-wall";
    public const string RoomWindowAreaTooLarge = "room.windows.area-too-large";

    public const string WallAdjacentRoomRequired = "wall.adjacent-room.required";
    public const string WallAdjacentRoomInvalid = "wall.adjacent-room.invalid";
    public const string WallAdjacentRoomSelfReference = "wall.adjacent-room.self-reference";
    public const string WallUnexpectedAdjacentRoomReference = "wall.adjacent-room.unexpected";

    public const string ThermalZoneRoomAssignedMultipleTimes = "thermal-zone.room-assigned-multiple-times";
    public const string AnnualClimateDataHoursInvalid = "climate.annual-hours.invalid";
    
    public const string RoomPeopleCountDefaultAvailable = "room.people-count.default-available";
    public const string RoomEquipmentLoadDefaultAvailable = "room.equipment-load.default-available";
    public const string RoomLightingLoadDefaultAvailable = "room.lighting-load.default-available";
    public const string RoomVentilationDefaultsAvailable = "room.ventilation.default-available";
}