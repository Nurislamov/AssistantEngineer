using AssistantEngineer.Modules.Calculations.Application.Contracts.Solar;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;

public sealed record Iso52016SurfaceWeatherSolarRecord(
    string SurfaceCode,
    SurfaceOrientation Orientation,
    double IncidenceAngleDegrees,
    double BeamIrradianceWm2,
    double DiffuseSkyIrradianceWm2,
    double GroundReflectedIrradianceWm2,
    double TotalIrradianceWm2);