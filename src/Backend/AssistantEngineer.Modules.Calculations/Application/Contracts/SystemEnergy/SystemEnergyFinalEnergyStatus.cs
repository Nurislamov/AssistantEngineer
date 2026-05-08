namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

public enum SystemEnergyFinalEnergyStatus
{
    Unknown = 0,
    Calculated = 1,
    PartiallyCalculated = 2,
    HandoffOnly = 3,
    NotCalculable = 4,
    Disabled = 5
}
