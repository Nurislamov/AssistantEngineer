namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;

public sealed record Iso52016RoomEnergySimulationRequest(
    string RoomCode,
    Iso52016WeatherSolarContext WeatherSolarContext,
    IReadOnlyList<Iso52016WindowSolarGainInput> Windows,
    int PeopleCount,
    double SensibleHeatGainPerPersonW,
    double EquipmentLoadW,
    double LightingLoadW,
    IReadOnlyList<double> OccupancyFactors,
    IReadOnlyList<double> EquipmentFactors,
    IReadOnlyList<double> LightingFactors,
    double TransmissionHeatTransferCoefficientWPerK,
    double VentilationHeatTransferCoefficientWPerK,
    double ThermalCapacityJPerK,
    double HeatingSetpointC,
    double CoolingSetpointC,
    Iso52016RoomHeatBalanceOptions? HeatBalanceOptions = null);