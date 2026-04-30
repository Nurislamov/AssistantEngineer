using AssistantEngineer.Modules.Calculations.Application.Contracts.InternalGains;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.SolarGains;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Transmission;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.RoomLoads;

public sealed record RoomLoadCalculationInput(
    int RoomId,
    string? RoomCode,
    string? RoomName,
    double AreaM2,
    double VolumeM3,
    double HeatingSetpointC,
    double CoolingSetpointC,
    double OutdoorDesignHeatingTemperatureC,
    double OutdoorDesignCoolingTemperatureC,
    IReadOnlyList<TransmissionElementInput>? TransmissionElements = null,
    IReadOnlyList<TransmissionElementInput>? CoolingTransmissionElements = null,
    RoomWindowSolarGainRequest? WindowSolarGains = null,
    VentilationAndInfiltrationLoadInput? HeatingVentilationAndInfiltration = null,
    VentilationAndInfiltrationLoadInput? CoolingVentilationAndInfiltration = null,
    InternalGainInput? InternalGains = null,
    RoomLoadFixedComponentInput? FixedComponents = null,
    IReadOnlyList<CalculationDiagnostic>? ApplicationDiagnostics = null,
    IReadOnlyList<string>? ApplicationAssumptions = null,
    RoomLoadCalculationMode CalculationMode = RoomLoadCalculationMode.DesignPoint,
    int? HourIndex = null,
    DateTimeOffset? Timestamp = null,
    string? DiagnosticsContext = null);
