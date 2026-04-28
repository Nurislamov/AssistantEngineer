namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;

public sealed record Iso52016RoomEnvelopeInput(
    double TransmissionHeatTransferCoefficientWPerK,
    double VentilationHeatTransferCoefficientWPerK,
    double ThermalCapacityJPerK);