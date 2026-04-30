namespace AssistantEngineer.Modules.Calculations.Application.Contracts.AnnualEnergy;

public sealed record AnnualEnergyComponentBreakdown(
    double TransmissionKWh,
    double VentilationKWh,
    double InfiltrationKWh,
    double SolarGainsKWh,
    double InternalGainsKWh,
    double GroundKWh,
    double NetTransmissionKWh = 0,
    double NetVentilationKWh = 0,
    double NetInfiltrationKWh = 0,
    double NetGroundKWh = 0);