namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Solar;

public sealed record SurfaceIrradianceResult(
    double IncidenceAngleDegrees,
    double BeamIrradianceWm2,
    double DiffuseSkyIrradianceWm2,
    double GroundReflectedIrradianceWm2,
    double TotalIrradianceWm2);