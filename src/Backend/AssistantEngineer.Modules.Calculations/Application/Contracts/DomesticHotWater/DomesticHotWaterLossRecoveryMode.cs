namespace AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;

public enum DomesticHotWaterLossRecoveryMode
{
    Unknown = 0,
    NonRecoverable = 1,
    RecoverableToHeatedSpace = 2,
    RecoverableToUnheatedSpace = 3,
    PartiallyRecoverable = 4,
    Other = 5
}
