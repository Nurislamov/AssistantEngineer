namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

public enum SystemEnergyLossOwnershipPolicy
{
    UpstreamOwnsLosses = 1,
    SystemEnergyOwnsLosses = 2,
    NoDoubleCounting = 3,
    ExplicitStageOwnership = 4
}
