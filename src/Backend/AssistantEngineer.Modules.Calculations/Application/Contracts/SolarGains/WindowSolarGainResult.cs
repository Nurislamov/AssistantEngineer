namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SolarGains;

public sealed record WindowSolarGainResult(
    int WindowId,
    int RoomId,
    double AreaM2,
    double OrientationAzimuthDeg,
    double TiltDeg,
    double IncidentIrradianceWPerM2,
    double DirectIrradianceWPerM2,
    double DiffuseIrradianceWPerM2,
    double GroundReflectedIrradianceWPerM2,
    double Shgc,
    double FrameFactor,
    double InternalShadingFactor,
    double ExternalShadingFactor,
    double FixedShadingFactor,
    double EffectiveSolarFactor,
    double DirectSolarGainW,
    double DiffuseSolarGainW,
    double GroundReflectedSolarGainW,
    double SolarGainW,
    int? HourIndex,
    bool IsIncludedInLoad,
    IReadOnlyList<SolarGainDiagnostic> Diagnostics)
{
    public bool HasErrors => Diagnostics.Any(diagnostic =>
        diagnostic.Severity == SolarGainDiagnosticSeverity.Error);
}
