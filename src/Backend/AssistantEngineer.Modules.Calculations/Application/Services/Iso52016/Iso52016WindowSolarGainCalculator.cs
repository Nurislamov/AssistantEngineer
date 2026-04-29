using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.SolarGains;
using AssistantEngineer.Modules.Calculations.Application.Contracts.WeatherSolar;
using AssistantEngineer.Modules.Calculations.Application.Services.SolarGains;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;

public sealed class Iso52016WindowSolarGainCalculator : IIso52016WindowSolarGainCalculator
{
    private readonly WindowSolarGainEngine _engine;

    public Iso52016WindowSolarGainCalculator(
        WindowSolarGainEngine? engine = null)
    {
        _engine = engine ?? new WindowSolarGainEngine();
    }

    public Result<Iso52016WindowSolarGainResult> Calculate(
        Iso52016WindowSolarGainRequest request)
    {
        var validation = Validate(request);

        if (validation.IsFailure)
            return Result<Iso52016WindowSolarGainResult>.Failure(validation);

        var surface = request.Hour.GetSurface(
            request.Orientation);
        var weatherSurface = WeatherSolarSurface.FromCardinalDirection(
            request.Orientation);
        var frameFactor = 1.0 - request.FrameFraction;

        var engineResult = _engine.Calculate(
            new WindowSolarGainInput(
                WindowId: 0,
                RoomId: 0,
                AreaM2: request.WindowAreaM2,
                OrientationAzimuthDeg: weatherSurface.Orientation.AzimuthDegrees,
                TiltDeg: weatherSurface.Orientation.TiltDegrees,
                Shgc: request.SolarHeatGainCoefficient,
                FrameFactor: frameFactor,
                InternalShadingFactor: 1.0,
                ExternalShadingFactor: request.ShadingFactor,
                FixedShadingFactor: 1.0,
                IncidentIrradianceWPerM2: surface.TotalIrradianceWm2,
                DirectIrradianceWPerM2: surface.BeamIrradianceWm2,
                DiffuseIrradianceWPerM2: surface.DiffuseSkyIrradianceWm2,
                GroundReflectedIrradianceWPerM2: surface.GroundReflectedIrradianceWm2,
                HourIndex: request.Hour.HourOfYear,
                IsNight: request.Hour.SolarAltitudeDegrees <= 0,
                DiagnosticsContext: $"Hour {request.Hour.HourOfYear} {surface.SurfaceCode} window"));

        if (engineResult.IsFailure)
            return Result<Iso52016WindowSolarGainResult>.Failure(engineResult);

        if (engineResult.Value.HasErrors)
        {
            var firstError = engineResult.Value.Diagnostics.First(diagnostic =>
                diagnostic.Severity == SolarGainDiagnosticSeverity.Error);
            return Result<Iso52016WindowSolarGainResult>.Validation(firstError.Message);
        }

        return Result<Iso52016WindowSolarGainResult>.Success(
            new Iso52016WindowSolarGainResult(
                HourOfYear: request.Hour.HourOfYear,
                Orientation: request.Orientation,
                SurfaceCode: surface.SurfaceCode,
                WindowAreaM2: request.WindowAreaM2,
                EffectiveGlazingAreaM2: request.WindowAreaM2 * frameFactor,
                SolarHeatGainCoefficient: request.SolarHeatGainCoefficient,
                ShadingFactor: request.ShadingFactor,
                BeamSolarGainW: engineResult.Value.DirectSolarGainW,
                DiffuseSkySolarGainW: engineResult.Value.DiffuseSolarGainW,
                GroundReflectedSolarGainW: engineResult.Value.GroundReflectedSolarGainW,
                TotalSolarGainW: engineResult.Value.SolarGainW,
                SurfaceTotalIrradianceWm2: surface.TotalIrradianceWm2,
                EffectiveSolarFactor: engineResult.Value.EffectiveSolarFactor,
                Diagnostics: engineResult.Value.Diagnostics));
    }

    private static Result Validate(
        Iso52016WindowSolarGainRequest request)
    {
        if (request.Hour is null)
            return Result.Validation("Hourly weather-solar record is required.");

        if (request.WindowAreaM2 <= 0)
            return Result.Validation("Window area must be greater than zero.");

        if (request.SolarHeatGainCoefficient is < 0.0 or > 1.0)
            return Result.Validation("Solar heat gain coefficient must be between 0 and 1.");

        if (request.FrameFraction is < 0.0 or >= 1.0)
            return Result.Validation("Frame fraction must be greater than or equal to 0 and less than 1.");

        if (request.ShadingFactor is < 0.0 or > 1.0)
            return Result.Validation("Shading factor must be between 0 and 1.");

        return Result.Success();
    }
}
