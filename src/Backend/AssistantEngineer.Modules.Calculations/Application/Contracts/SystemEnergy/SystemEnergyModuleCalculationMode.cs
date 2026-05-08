namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

public enum SystemEnergyModuleCalculationMode
{
    Unknown = 0,
    Disabled = 1,
    DirectProfile = 2,
    LossFraction = 3,
    FixedEfficiency = 4,
    FixedLoss = 5,
    CoefficientBased = 6,
    HandoffOnly = 7,
    Other = 8
}
