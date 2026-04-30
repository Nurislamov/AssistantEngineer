namespace AssistantEngineer.Modules.Calculations.Application.Contracts.AnnualEnergy;

public sealed record AnnualEnergyComponentBreakdown(
    double TransmissionKWh,
    double VentilationKWh,
    double InfiltrationKWh,
    double SolarGainsKWh,
    double InternalGainsKWh,
    double GroundKWh);
