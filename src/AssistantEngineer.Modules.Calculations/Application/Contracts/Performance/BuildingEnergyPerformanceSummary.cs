using AssistantEngineer.Modules.Calculations.Application.Services.Performance;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Performance;

public sealed record BuildingEnergyPerformanceSummary(
    int BuildingId,
    string BuildingName,
    int Year,
    double FloorAreaM2,
    IReadOnlyList<BuildingEnergyEndUseSummary> EndUses,
    double TotalUsefulEnergyKWh,
    double TotalFinalEnergyKWh,
    double TotalPrimaryEnergyKWh,
    double TotalCo2Kg,
    double FinalEnergyIntensityKWhPerM2Year,
    double PrimaryEnergyIntensityKWhPerM2Year,
    double Co2IntensityKgPerM2Year);

public sealed record BuildingEnergyEndUseSummary(
    BuildingEnergyEndUse EndUse,
    EnergyCarrierType Carrier,
    double UsefulEnergyKWh,
    double FinalEnergyKWh,
    double PrimaryEnergyFactor,
    double Co2KgPerKWh,
    double PrimaryEnergyKWh,
    double Co2Kg,
    bool HasInvalidCarrier);

public enum BuildingEnergyEndUse
{
    Heating,
    Cooling,
    DomesticHotWater
}