namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SolarGains;

public sealed record WindowSolarGainInput(
    int WindowId,
    int RoomId,
    double AreaM2,
    double OrientationAzimuthDeg,
    double TiltDeg,
    double? Shgc,
    double? FrameFactor = null,
    double InternalShadingFactor = 1.0,
    double ExternalShadingFactor = 1.0,
    double FixedShadingFactor = 1.0,
    double? IncidentIrradianceWPerM2 = null,
    double? DirectIrradianceWPerM2 = null,
    double? DiffuseIrradianceWPerM2 = null,
    double? GroundReflectedIrradianceWPerM2 = null,
    int? HourIndex = null,
    bool IsNight = false,
    string? DiagnosticsContext = null);
