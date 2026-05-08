namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

public enum SystemEnergyLoadSplitMode
{
    Unknown = 0,
    SingleGenerator = 1,
    PriorityOrder = 2,
    FixedFraction = 3,
    CapacityLimitedPriority = 4,
    CustomHourlyFraction = 5,
    Other = 6
}
