namespace AssistantEngineer.Modules.Calculations.Application.Models.Heating;

public sealed record BuildingHeatingReadModel(
    int BuildingId,
    string BuildingName,
    int ProjectId,
    string ProjectName,
    double? WinterDesignTemperatureC,
    IReadOnlyList<RoomHeatingReadModel> Rooms);

public sealed record RoomHeatingReadModel(
    int RoomId,
    string RoomName,
    double AreaM2,
    double HeightM,
    double IndoorTemperatureC,
    double? OutdoorTemperatureOverrideC,
    HeatingVentilationReadModel? Ventilation,
    IReadOnlyList<WindowHeatingReadModel> Windows,
    IReadOnlyList<WallHeatingReadModel> Walls)
{
    public double VolumeM3 => AreaM2 * HeightM;
}

public sealed record HeatingVentilationReadModel(
    double AirChangesPerHour,
    double HeatRecoveryEfficiency,
    double InfiltrationAirChangesPerHour,
    double StackCoefficient);

public sealed record WindowHeatingReadModel(
    double AreaM2,
    double UValue);

public sealed record WallHeatingReadModel(
    double AreaM2,
    bool IsExternal,
    double UValue,
    IReadOnlyList<ConstructionLayerHeatingReadModel> ConstructionLayers);

public sealed record ConstructionLayerHeatingReadModel(
    double ThicknessM,
    double ThermalConductivityWPerMK);
