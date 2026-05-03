using AssistantEngineer.Modules.Calculations.Application.Abstractions.Solar;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Solar;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Solar;

internal sealed class PerezAnisotropicSurfaceIrradianceCalculator : ISurfaceIrradianceCalculator
{
    private const double SolarConstantWm2 = 1370.0;
    private const double PerezClearnessK = 1.104;

    private static readonly double[,] PerezCoefficientTable =
    {
        { 1.065, -0.008,  0.588, -0.062, -0.060,  0.072, -0.022 },
        { 1.230,  0.130,  0.683, -0.151, -0.019,  0.066, -0.029 },
        { 1.500,  0.330,  0.487, -0.221,  0.055, -0.064, -0.026 },
        { 1.950,  0.568,  0.187, -0.295,  0.109, -0.152, -0.014 },
        { 2.800,  0.873, -0.392, -0.362,  0.226, -0.462,  0.001 },
        { 4.500,  1.132, -1.237, -0.412,  0.288, -0.823,  0.056 },
        { 6.200,  1.060, -1.600, -0.359,  0.264, -1.127,  0.131 },
        { 99999,  0.678, -0.327, -0.250,  0.156, -1.377,  0.251 }
    };

    public SurfaceIrradianceResult Calculate(
        SurfaceIrradianceRequest request)
    {
        Validate(request);

        var diagnostics = new List<CalculationDiagnostic>();

        var geometry = CalculateGeometry(
            request.Surface,
            request.SolarPosition);

        diagnostics.Add(new CalculationDiagnostic(
            CalculationDiagnosticSeverity.Info,
            "SolarWeather.PerezAnisotropicModelUsed",
            "Surface irradiance was calculated with the Perez anisotropic sky model.",
            request.DiagnosticsContext));

        diagnostics.Add(new CalculationDiagnostic(
            CalculationDiagnosticSeverity.Info,
            "SolarWeather.SurfaceIrradianceCalculated",
            $"Perez surface irradiance calculated from DNI {request.DirectNormalIrradianceWm2:0.###} W/m2, DHI {request.DiffuseHorizontalIrradianceWm2:0.###} W/m2 and GHI {request.GlobalHorizontalIrradianceWm2:0.###} W/m2.",
            request.DiagnosticsContext));

        if (request.SolarPosition.SolarAltitudeDegrees <= 0)
        {
            diagnostics.Add(new CalculationDiagnostic(
                CalculationDiagnosticSeverity.Info,
                "SolarWeather.NightSolarClampedToZero",
                "Solar altitude is below or equal to the horizon; direct, diffuse, ground-reflected and total surface irradiance were clamped to zero.",
                request.DiagnosticsContext));

            return new SurfaceIrradianceResult(
                IncidenceAngleDegrees: geometry.IncidenceAngleDegrees,
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
                "Global horizontal irradiance is available, but direct normal and diffuse horizontal irradiance are both zero; Perez surface irradiance cannot split beam and diffuse components.",
                request.DiagnosticsContext));
        }

        var globalHorizontalIrradiance = ResolveGlobalHorizontalIrradiance(
            request,
            geometry);

        var components = CalculatePerezComponents(
            request,
            geometry,
            globalHorizontalIrradiance);

        diagnostics.Add(new CalculationDiagnostic(
            CalculationDiagnosticSeverity.Info,
            "SolarWeather.PerezSkyState",
            $"Perez clearness is {components.Clearness:0.###}, brightness is {components.SkyBrightness:0.###}, F1 is {components.F1:0.###}, F2 is {components.F2:0.###}.",
            request.DiagnosticsContext));

        return new SurfaceIrradianceResult(
            IncidenceAngleDegrees: geometry.IncidenceAngleDegrees,
            BeamIrradianceWm2: components.BeamIrradianceWm2,
            DiffuseSkyIrradianceWm2: components.DiffuseSkyIrradianceWm2,
            GroundReflectedIrradianceWm2: components.GroundReflectedIrradianceWm2,
            TotalIrradianceWm2: components.TotalIrradianceWm2)
        {
            Diagnostics = diagnostics
        };
    }

    private static PerezIrradianceComponents CalculatePerezComponents(
        SurfaceIrradianceRequest request,
        SurfaceSolarGeometry geometry,
        double globalHorizontalIrradianceWm2)
    {
        var directProjectedWm2 =
            request.DirectNormalIrradianceWm2 *
            SolarMath.PositiveOrZero(geometry.CosIncidence);

        var groundReflectedWm2 =
            globalHorizontalIrradianceWm2 *
            request.GroundReflectance *
            (1.0 - Math.Cos(geometry.TiltRadians)) /
            2.0;

        if (request.DiffuseHorizontalIrradianceWm2 <= 0)
        {
            var totalWithoutDiffuse =
                directProjectedWm2 +
                groundReflectedWm2;

            return new PerezIrradianceComponents(
                BeamIrradianceWm2: Math.Max(0, directProjectedWm2),
                DiffuseSkyIrradianceWm2: 0,
                GroundReflectedIrradianceWm2: Math.Max(0, groundReflectedWm2),
                TotalIrradianceWm2: Math.Max(0, totalWithoutDiffuse),
                Clearness: 0,
                SkyBrightness: 0,
                F1: 0,
                F2: 0);
        }

        var extraterrestrialIrradianceWm2 =
            SolarConstantWm2 *
            (
                1.0 +
                0.033 *
                Math.Cos(
                    2.0 *
                    Math.PI *
                    request.SolarPosition.DayOfYear /
                    365.0)
            );

        var solarAltitudeRadians = SolarMath.ToRadians(
            request.SolarPosition.SolarAltitudeDegrees);

        var clearness =
            (
                (request.DiffuseHorizontalIrradianceWm2 + request.DirectNormalIrradianceWm2) /
                request.DiffuseHorizontalIrradianceWm2 +
                PerezClearnessK * Math.Pow(solarAltitudeRadians, 3.0)
            ) /
            (
                1.0 +
                PerezClearnessK * Math.Pow(solarAltitudeRadians, 3.0)
            );

        var skyBrightness =
            extraterrestrialIrradianceWm2 > 0
                ? request.SolarPosition.RelativeAirMass *
                  request.DiffuseHorizontalIrradianceWm2 /
                  extraterrestrialIrradianceWm2
                : 0;

        var coefficients = SelectPerezCoefficients(clearness);

        var f1 =
            Math.Max(
                0,
                coefficients.F11 +
                coefficients.F12 * skyBrightness +
                coefficients.F13 * geometry.ZenithRadians);

        var f2 =
            coefficients.F21 +
            coefficients.F22 * skyBrightness +
            coefficients.F23 * geometry.ZenithRadians;

        var aPerez =
            SolarMath.PositiveOrZero(geometry.CosIncidence);

        var bPerez =
            Math.Max(
                Math.Cos(SolarMath.ToRadians(85.0)),
                Math.Cos(geometry.ZenithRadians));

        var diffuseSkyWithCircumsolarWm2 =
            request.DiffuseHorizontalIrradianceWm2 *
            (
                (1.0 - f1) *
                (1.0 + Math.Cos(geometry.TiltRadians)) /
                2.0 +
                f1 *
                aPerez /
                bPerez +
                f2 *
                Math.Sin(geometry.TiltRadians)
            );

        var circumsolarWm2 =
            request.DiffuseHorizontalIrradianceWm2 *
            f1 *
            aPerez /
            bPerez;

        var beamWm2 =
            directProjectedWm2 +
            circumsolarWm2;

        var diffuseSkyWm2 =
            diffuseSkyWithCircumsolarWm2 -
            circumsolarWm2;

        beamWm2 = Math.Max(0, beamWm2);
        diffuseSkyWm2 = Math.Max(0, diffuseSkyWm2);
        groundReflectedWm2 = Math.Max(0, groundReflectedWm2);

        return new PerezIrradianceComponents(
            BeamIrradianceWm2: beamWm2,
            DiffuseSkyIrradianceWm2: diffuseSkyWm2,
            GroundReflectedIrradianceWm2: groundReflectedWm2,
            TotalIrradianceWm2: Math.Max(0, beamWm2 + diffuseSkyWm2 + groundReflectedWm2),
            Clearness: clearness,
            SkyBrightness: skyBrightness,
            F1: f1,
            F2: f2);
    }

    private static PerezCoefficientSet SelectPerezCoefficients(
        double clearness)
    {
        for (var row = 0; row < PerezCoefficientTable.GetLength(0); row++)
        {
            if (clearness < PerezCoefficientTable[row, 0])
            {
                return new PerezCoefficientSet(
                    F11: PerezCoefficientTable[row, 1],
                    F12: PerezCoefficientTable[row, 2],
                    F13: PerezCoefficientTable[row, 3],
                    F21: PerezCoefficientTable[row, 4],
                    F22: PerezCoefficientTable[row, 5],
                    F23: PerezCoefficientTable[row, 6]);
            }
        }

        var lastRow = PerezCoefficientTable.GetLength(0) - 1;

        return new PerezCoefficientSet(
            F11: PerezCoefficientTable[lastRow, 1],
            F12: PerezCoefficientTable[lastRow, 2],
            F13: PerezCoefficientTable[lastRow, 3],
            F21: PerezCoefficientTable[lastRow, 4],
            F22: PerezCoefficientTable[lastRow, 5],
            F23: PerezCoefficientTable[lastRow, 6]);
    }

    private static SurfaceSolarGeometry CalculateGeometry(
        SurfaceOrientation surface,
        SolarPositionResult solarPosition)
    {
        var tiltRadians = SolarMath.ToRadians(
            surface.TiltDegrees);

        var zenithRadians = SolarMath.ToRadians(
            solarPosition.ZenithAngleDegrees);

        var solarAzimuthRadians = SolarMath.ToRadians(
            solarPosition.SolarAzimuthDegrees);

        var surfaceAzimuthRadians = SolarMath.ToRadians(
            SolarMath.NormalizeDegrees360(surface.AzimuthDegrees));

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

        return new SurfaceSolarGeometry(
            TiltRadians: tiltRadians,
            ZenithRadians: zenithRadians,
            CosIncidence: cosIncidence,
            IncidenceAngleDegrees: incidenceAngleDegrees);
    }

    private static double ResolveGlobalHorizontalIrradiance(
        SurfaceIrradianceRequest request,
        SurfaceSolarGeometry geometry)
    {
        if (request.GlobalHorizontalIrradianceWm2 > 0)
            return request.GlobalHorizontalIrradianceWm2;

        var projectedDirect =
            request.DirectNormalIrradianceWm2 *
            Math.Max(0, Math.Cos(geometry.ZenithRadians));

        return Math.Max(
            0,
            projectedDirect + request.DiffuseHorizontalIrradianceWm2);
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

    private sealed record SurfaceSolarGeometry(
        double TiltRadians,
        double ZenithRadians,
        double CosIncidence,
        double IncidenceAngleDegrees);

    private sealed record PerezCoefficientSet(
        double F11,
        double F12,
        double F13,
        double F21,
        double F22,
        double F23);

    private sealed record PerezIrradianceComponents(
        double BeamIrradianceWm2,
        double DiffuseSkyIrradianceWm2,
        double GroundReflectedIrradianceWm2,
        double TotalIrradianceWm2,
        double Clearness,
        double SkyBrightness,
        double F1,
        double F2);
}
