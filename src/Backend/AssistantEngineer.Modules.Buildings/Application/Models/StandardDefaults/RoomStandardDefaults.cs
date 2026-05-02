namespace AssistantEngineer.Modules.Buildings.Application.Models.StandardDefaults;

public sealed class RoomStandardDefaults
{
    public int SuggestedPeopleCount { get; set; }
    public double EquipmentLoadWatts { get; set; }
    public double LightingLoadWatts { get; set; }
    public double MinimumVentilationLitersPerSecondM2 { get; set; }
    public double OutdoorAirLitersPerSecondPerPerson { get; set; }
    public double OutdoorAirLitersPerSecondPerM2 { get; set; }
    public bool HasDhwDefaults { get; set; }
    public string SourceTableVersion { get; set; } = string.Empty;
}