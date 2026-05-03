namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Matrix;

/// <summary>
/// Seed reference data for sensible internal gains. Values are intentionally explicit and replaceable by project data,
/// national annex values or a later ISO 16798/52016 profile table import.
/// </summary>
public sealed record Iso52016InternalGainReferenceData(
    string UseType,
    double OccupantDensityPersonPerM2,
    double SensibleHeatPerPersonW,
    double LightingPowerDensityWPerM2,
    double EquipmentPowerDensityWPerM2,
    double ConvectiveFraction,
    double RadiativeFraction,
    string SourceNote);