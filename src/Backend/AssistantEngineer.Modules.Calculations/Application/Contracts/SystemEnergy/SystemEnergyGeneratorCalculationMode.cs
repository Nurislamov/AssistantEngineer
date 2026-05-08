namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

public enum SystemEnergyGeneratorCalculationMode
{
    Unknown = 0,
    Disabled = 1,
    FixedEfficiency = 2,
    FixedCop = 3,
    FixedEer = 4,
    SeasonalPerformanceFactor = 5,
    DirectFinalEnergyProfile = 6,
    DistrictHandoff = 7,
    CustomFactor = 8,
    HandoffOnly = 9,
    Other = 10
}
