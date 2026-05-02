namespace AssistantEngineer.Infrastructure.Seeding;

public sealed class DevelopmentDemoSeedResult
{
    public int ClimateZoneId { get; init; }
    public int ProjectId { get; init; }
    public int BuildingId { get; init; }
    public int FloorId { get; init; }
    public int RoomId { get; init; }
    public int WallId { get; init; }
    public int WindowId { get; init; }
    public int VentilationParametersId { get; init; }
    public int WeatherYear { get; init; }
    public IReadOnlyList<int> EquipmentCatalogItemIds { get; init; } = [];
}
