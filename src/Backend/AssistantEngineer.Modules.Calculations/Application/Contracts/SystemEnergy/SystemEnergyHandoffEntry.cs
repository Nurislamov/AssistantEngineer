using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy.En15316;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

public sealed record SystemEnergyHandoffEntry(
    int TimeStepIndex,
    int Month,
    SystemEnergyHandoffEnergyServiceType EnergyServiceType,
    double UsefulEnergyKWh,
    En15316EnergyCarrier Carrier);
