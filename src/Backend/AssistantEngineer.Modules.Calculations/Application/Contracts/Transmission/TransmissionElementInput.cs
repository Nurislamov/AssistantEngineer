namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Transmission;

public sealed record TransmissionElementInput(
    int ElementId,
    TransmissionElementType ElementType,
    int RoomId,
    double AreaM2,
    double UValueWPerM2K,
    double IndoorTemperatureC,
    TransmissionBoundaryType BoundaryType,
    double? OutdoorTemperatureC = null,
    double? BoundaryTemperatureC = null,
    double? AdjacentTemperatureC = null,
    double? GroundTemperatureC = null,
    double? CorrectionFactor = null,
    string? DiagnosticsContext = null);
