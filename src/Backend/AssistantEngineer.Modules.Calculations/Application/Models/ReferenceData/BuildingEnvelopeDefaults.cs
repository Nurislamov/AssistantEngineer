namespace AssistantEngineer.Modules.Calculations.Application.Models.ReferenceData;

public sealed record BuildingEnvelopeDefaults(
    double FloorUValueWPerM2K,
    double CeilingUValueWPerM2K,
    double FloorHeatCapacityKjPerM2K,
    double CeilingHeatCapacityKjPerM2K);
