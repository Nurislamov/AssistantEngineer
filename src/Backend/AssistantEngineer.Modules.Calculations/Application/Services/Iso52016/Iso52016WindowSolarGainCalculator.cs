using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;

public sealed class Iso52016WindowSolarGainCalculator : IIso52016WindowSolarGainCalculator
{
    public Result<Iso52016WindowSolarGainResult> Calculate(
        Iso52016WindowSolarGainRequest request)
    {
        var validation = Validate(request);

        if (validation.IsFailure)
            return Result<Iso52016WindowSolarGainResult>.Failure(validation);

        var surface = request.Hour.GetSurface(
            request.Orientation);

        var effectiveGlazingArea =
            request.WindowAreaM2 *
            (1.0 - request.FrameFraction);

        var transmissionFactor =
            effectiveGlazingArea *
            request.SolarHeatGainCoefficient *
            request.ShadingFactor;

        var beamGain =
            surface.BeamIrradianceWm2 *
            transmissionFactor;

        var diffuseSkyGain =
            surface.DiffuseSkyIrradianceWm2 *
            transmissionFactor;

        var groundReflectedGain =
            surface.GroundReflectedIrradianceWm2 *
            transmissionFactor;

        var totalGain =
            beamGain +
            diffuseSkyGain +
            groundReflectedGain;

        return Result<Iso52016WindowSolarGainResult>.Success(
            new Iso52016WindowSolarGainResult(
                HourOfYear: request.Hour.HourOfYear,
                Orientation: request.Orientation,
                SurfaceCode: surface.SurfaceCode,
                WindowAreaM2: request.WindowAreaM2,
                EffectiveGlazingAreaM2: effectiveGlazingArea,
                SolarHeatGainCoefficient: request.SolarHeatGainCoefficient,
                ShadingFactor: request.ShadingFactor,
                BeamSolarGainW: beamGain,
                DiffuseSkySolarGainW: diffuseSkyGain,
                GroundReflectedSolarGainW: groundReflectedGain,
                TotalSolarGainW: totalGain,
                SurfaceTotalIrradianceWm2: surface.TotalIrradianceWm2));
    }

    private static Result Validate(
        Iso52016WindowSolarGainRequest request)
    {
        if (request.Hour is null)
            return Result.Validation("ISO 52016 hourly weather-solar record is required.");

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