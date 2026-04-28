namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Solar;

public sealed record SurfaceIrradianceRequest(
    SolarPositionResult SolarPosition,
    SurfaceOrientation Surface,
    double DirectNormalIrradianceWm2,
    double DiffuseHorizontalIrradianceWm2,
    double GlobalHorizontalIrradianceWm2,
    double GroundReflectance = 0.2);