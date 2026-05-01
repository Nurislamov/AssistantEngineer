using AssistantEngineer.Modules.Calculations.Application.Abstractions.Solar;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Solar;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Solar;

internal sealed class IsotropicSkySurfaceIrradianceCalculator : ISurfaceIrradianceCalculator
{
    public SurfaceIrradianceResult Calculate(
        SurfaceIrradianceRequest request)
    {
        Validate(request);

        var diagnostics = new List<CalculationDiagnostic>();
        var tiltRadians = SolarMath.ToRadians(
            request.Surface.TiltDegrees);

        var zenithRadians = SolarMath.ToRadians(
            request.SolarPosition.ZenithAngleDegrees);

        var solarAzimuthRadians = SolarMath.ToRadians(
            request.SolarPosition.SolarAzimuthDegrees);

        var surfaceAzimuthRadians = SolarMath.ToRadians(
            SolarMath.NormalizeDegrees360(request.Surface.AzimuthDegrees));

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

        diagnostics.Add(new CalculationDiagnostic(
            CalculationDiagnosticSeverity.Info,
            "SolarWeather.SurfaceIrradianceCalculated",
            $"Surface irradiance calculated from DNI {request.DirectNormalIrradianceWm2:0.###} W/m2, DHI {request.DiffuseHorizontalIrradianceWm2:0.###} W/m2 and GHI {request.GlobalHorizontalIrradianceWm2:0.###} W/m2.",
            request.DiagnosticsContext));

        if (request.SolarPosition.SolarAltitudeDegrees <= 0)
        {
            diagnostics.Add(new CalculationDiagnostic(
                CalculationDiagnosticSeverity.Info,
                "SolarWeather.NightSolarClampedToZero",
                "Solar altitude is below or equal to the horizon; direct, diffuse, ground-reflected and total surface irradiance were clamped to zero.",
                request.DiagnosticsContext));

            return new SurfaceIrradianceResult(
                IncidenceAngleDegrees: incidenceAngleDegrees,
                BeamIrradianceWm2: 0,
                DiffuseSkyIrradianceWm2: 0,
                GroundReflectedIrradianceWm2: 0,
                TotalIrradianceWm2: 0)
            {
                Diagnostics = diagnostics
            };
        }

        if (request.GlobalHorizontalIrradianceWm2 > 0 &&
            request.DirectNormalIrradianceWm2 == 0 &&
            request.DiffuseHorizontalIrradianceWm2 == 0)
        {
            diagnostics.Add(new CalculationDiagnostic(
                CalculationDiagnosticSeverity.Warning,
                "SolarWeather.MissingDirectDiffuseSolarData",
                "Global horizontal irradiance is available, but direct normal and diffuse horizontal irradiance are both zero; surface irradiance cannot split beam and diffuse components.",
                request.DiagnosticsContext));
        }

        var beamIrradiance =
            request.DirectNormalIrradianceWm2 *
            SolarMath.PositiveOrZero(cosIncidence);

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
            TotalIrradianceWm2: Math.Max(0, totalIrradiance))
        {
            Diagnostics = diagnostics
        };
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
