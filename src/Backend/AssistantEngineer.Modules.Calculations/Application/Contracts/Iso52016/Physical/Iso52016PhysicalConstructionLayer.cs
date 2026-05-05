namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Physical;

/// <summary>
/// Construction layer input used by the ISO52016-inspired physical surface adapter.
/// The layer is used for deterministic engineering estimates of conductance and heat capacity.
/// </summary>
public sealed record Iso52016PhysicalConstructionLayer(
    string LayerId,
    double ThicknessM,
    double ConductivityWPerMK,
    double DensityKgPerM3,
    double SpecificHeatCapacityJPerKgK);