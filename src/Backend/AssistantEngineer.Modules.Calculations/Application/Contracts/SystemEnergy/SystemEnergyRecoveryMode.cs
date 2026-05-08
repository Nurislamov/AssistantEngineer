namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

public enum SystemEnergyRecoveryMode
{
    Unknown = 0,
    NonRecoverable = 1,
    RecoverableToHeatedSpace = 2,
    RecoverableToUnheatedSpace = 3,
    PartiallyRecoverable = 4,
    Other = 5
}
