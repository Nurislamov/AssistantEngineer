namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Physical;

/// <summary>
/// Physical construction layer used by the ISO52016-inspired surface expansion builder.
/// This is an application-side engineering contract, not copied external implementation code.
/// </summary>
public sealed record Iso52016PhysicalConstructionLayer(
    string LayerId,
    double ThicknessM,
    double ConductivityWPerMK,
    double DensityKgPerM3,
    double SpecificHeatCapacityJPerKgK)
{
    public double ThermalResistanceM2KPerW => ThicknessM / ConductivityWPerMK;

    public double HeatCapacityJPerM2K =>
        ThicknessM * DensityKgPerM3 * SpecificHeatCapacityJPerKgK;
}