namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy.En15316;

public sealed record HeatingCircuit(
    string CircuitId,
    HeatingCircuitType CircuitType,
    EmissionSystemModel Emission,
    DistributionCircuitModel Distribution,
    GenerationSystemModel Generation,
    StorageSystemModel Storage,
    FlowReturnTemperaturePair DesignFlowReturnTemperatureC);
