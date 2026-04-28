using AssistantEngineer.Modules.Calculations.Application.Abstractions.Solar;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Solar;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Solar;

internal sealed class IsotropicSkySurfaceIrradianceCalculator : ISurfaceIrradianceCalculator
{
    public SurfaceIrradianceResult Calculate(
        SurfaceIrradianceRequest request)
    {
        Validate(request);

        var tiltRadians = SolarMath.ToRadians(
            request.Surface.TiltDegrees);

        var zenithRadians = SolarMath.ToRadians(
            request.SolarPosition.ZenithAngleDegrees);

        var solarAzimuthRadians = SolarMath.ToRadians(
            request.SolarPosition.SolarAzimuthDegrees);

        var surfaceAzimuthRadians = SolarMath.ToRadians(
            request.Surface.AzimuthDegrees);

        var cosIncidence =
            Math.Cos(zenithRadians) * Math.Cos(tiltRadians) +
            Math.Sin(zenithRadians) * Math.Sin(tiltRadians) *
            Math.Cos(solarAzimuthRadians - surfaceAzimuthRadians);

        cosIncidence = SolarMath.Clamp(
            cosIncidence,
            -1.0,
            1.0);

        var incidenceAngleDegrees = SolarMath.ToDegrees(
            Math.Acos(cosIncidence));

        var beamIrradiance = request.SolarPosition.SolarAltitudeDegrees <= 0
            ? 0.0
            : request.DirectNormalIrradianceWm2 * SolarMath.PositiveOrZero(cosIncidence);

        var diffuseSkyIrradiance =
            request.DiffuseHorizontalIrradianceWm2 *
            (1.0 + Math.Cos(tiltRadians)) /
            2.0;

        var groundReflectedIrradiance =
            request.GlobalHorizontalIrradianceWm2 *
            request.GroundReflectance *
            (1.0 - Math.Cos(tiltRadians)) /
            2.0;

        var totalIrradiance =
            beamIrradiance +
            diffuseSkyIrradiance +
            groundReflectedIrradiance;

        return new SurfaceIrradianceResult(
            IncidenceAngleDegrees: incidenceAngleDegrees,
            BeamIrradianceWm2: beamIrradiance,
            DiffuseSkyIrradianceWm2: diffuseSkyIrradiance,
            GroundReflectedIrradianceWm2: groundReflectedIrradiance,
            TotalIrradianceWm2: totalIrradiance);
    }

    private static void Validate(
        SurfaceIrradianceRequest request)
    {
        if (request.Surface.TiltDegrees is < 0.0 or > 180.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                "Surface tilt must be between 0 and 180 degrees.");
        }

        if (request.DirectNormalIrradianceWm2 < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                "Direct normal irradiance must not be negative.");
        }

        if (request.DiffuseHorizontalIrradianceWm2 < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                "Diffuse horizontal irradiance must not be negative.");
        }

        if (request.GlobalHorizontalIrradianceWm2 < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                "Global horizontal irradiance must not be negative.");
        }

        if (request.GroundReflectance is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                "Ground reflectance must be between 0 and 1.");
        }
    }
}