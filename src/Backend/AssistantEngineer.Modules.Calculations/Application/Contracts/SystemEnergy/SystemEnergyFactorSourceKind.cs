namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

public enum SystemEnergyFactorSourceKind
{
    Unknown = 0,
    UserProvided = 1,
    ProjectDefault = 2,
    InternalReference = 3,
    NationalAnnexPlaceholder = 4,
    ExternalDataset = 5,
    Other = 6
}
