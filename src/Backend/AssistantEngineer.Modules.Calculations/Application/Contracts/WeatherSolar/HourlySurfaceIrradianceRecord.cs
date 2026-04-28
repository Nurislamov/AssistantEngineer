using AssistantEngineer.Modules.Calculations.Application.Contracts.Solar;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.WeatherSolar;

public sealed record HourlySurfaceIrradianceRecord(
    string SurfaceCode,
    SurfaceOrientation Orientation,
    SurfaceIrradianceResult Irradiance);