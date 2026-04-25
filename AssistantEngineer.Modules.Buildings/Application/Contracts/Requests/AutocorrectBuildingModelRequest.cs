namespace AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;

public sealed class AutocorrectBuildingModelRequest
{
    public double MinimumRoomAreaM2 { get; set; } = 1.0;
    public double DefaultRoomHeightM { get; set; } = 2.7;
    public double MaximumWindowToFloorAreaRatio { get; set; } = 0.8;

    public bool ClampNegativePeopleCountToZero { get; set; } = true;
    public bool ClampNegativeLoadsToZero { get; set; } = true;
    public bool RemoveUnexpectedAdjacentRoomReferences { get; set; } = true;
    public bool ResizeOversizedWindows { get; set; } = true;
    public bool RemoveDuplicateThermalZoneAssignments { get; set; } = true;

    public bool ApplyRoomTypeInternalLoadDefaults { get; set; } = true;
    public bool ApplyPeopleCountDefaultsWhenMissing { get; set; } = true;
    public bool ApplyEquipmentLoadDefaultsWhenMissing { get; set; } = true;
    public bool ApplyLightingLoadDefaultsWhenMissing { get; set; } = true;

    public bool ApplyVentilationDefaultsWhenMissing { get; set; } = true;
}