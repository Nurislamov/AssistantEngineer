namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

public enum SystemEnergyModuleKind
{
    Unknown = 0,
    UsefulDemand = 1,
    Emission = 2,
    Control = 3,
    Distribution = 4,
    Storage = 5,
    Generation = 6,
    Auxiliary = 7,
    Recovery = 8,
    Handoff = 9,
    Other = 10
}
