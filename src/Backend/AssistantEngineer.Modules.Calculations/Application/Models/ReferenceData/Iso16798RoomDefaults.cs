namespace AssistantEngineer.Modules.Calculations.Application.Models.ReferenceData;

public sealed record Iso16798RoomDefaults(
    double SensibleHeatGainPerPersonW,
    double EquipmentGainWPerM2,
    double LightingGainWPerM2,
    double MinimumVentilationLitersPerSecondM2);