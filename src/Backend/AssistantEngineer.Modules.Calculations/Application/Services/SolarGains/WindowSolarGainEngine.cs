using AssistantEngineer.Modules.Calculations.Application.Contracts.SolarGains;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.SolarGains;

public sealed class WindowSolarGainEngine
{
    public Result<WindowSolarGainResult> Calculate(
        WindowSolarGainInput input)
    {
        if (input is null)
            return Result<WindowSolarGainResult>.Validation("Window solar gain input is required.");

        var diagnostics = Validate(input);
        var frameFactor = input.FrameFactor;

        if (!frameFactor.HasValue)
        {
            frameFactor = 1.0;
            diagnostics.Add(new SolarGainDiagnostic(
                SolarGainDiagnosticSeverity.Warning,
                "SolarGains.FrameFactorDefaulted",
                "Frame factor was not supplied and defaulted to 1.0.",
                input.DiagnosticsContext));
        }

        if (diagnostics.Any(diagnostic => diagnostic.Severity == SolarGainDiagnosticSeverity.Error))
            return Result<WindowSolarGainResult>.Success(Excluded(input, frameFactor ?? 0.0, diagnostics));

        var irradiance = ResolveIrradiance(input, diagnostics);
        if (irradiance is null)
            return Result<WindowSolarGainResult>.Success(Excluded(input, frameFactor.Value, diagnostics));

        var shgc = input.Shgc!.Value;
        var effectiveSolarFactor =
            shgc *
            frameFactor.Value *
            input.InternalShadingFactor *
            input.ExternalShadingFactor *
            input.FixedShadingFactor;

        diagnostics.Add(new SolarGainDiagnostic(
            SolarGainDiagnosticSeverity.Info,
            "SolarGains.EffectiveSolarFactor",
            $"Effective solar factor is {Round(effectiveSolarFactor)}.",
            input.DiagnosticsContext));

        if (input.IsNight || irradiance.IncidentIrradianceWPerM2 <= 0)
        {
            if (input.IsNight)
            {
                diagnostics.Add(new SolarGainDiagnostic(
                    SolarGainDiagnosticSeverity.Info,
                    "SolarWeather.NightSolarClampedToZero",
                    "Window solar gain was clamped to zero because the hour is marked as night.",
                    input.DiagnosticsContext));
            }

            diagnostics.Add(new SolarGainDiagnostic(
                SolarGainDiagnosticSeverity.Info,
                input.IsNight ? "SolarGains.Night" : "SolarGains.ZeroIrradiance",
                input.IsNight
                    ? "Solar gain is zero because the hour is marked as night."
                    : "Solar gain is zero because incident irradiance is zero.",
                input.DiagnosticsContext));

            return Result<WindowSolarGainResult>.Success(
                new WindowSolarGainResult(
                    input.WindowId,
                    input.RoomId,
                    input.AreaM2,
                    input.OrientationAzimuthDeg,
                    input.TiltDeg,
                    Round(irradiance.IncidentIrradianceWPerM2),
                    Round(irradiance.DirectIrradianceWPerM2),
                    Round(irradiance.DiffuseIrradianceWPerM2),
                    Round(irradiance.GroundReflectedIrradianceWPerM2),
                    shgc,
                    frameFactor.Value,
                    input.InternalShadingFactor,
                    input.ExternalShadingFactor,
                    input.FixedShadingFactor,
                    Round(effectiveSolarFactor),
                    DirectSolarGainW: 0,
                    DiffuseSolarGainW: 0,
                    GroundReflectedSolarGainW: 0,
                    SolarGainW: 0,
                    input.HourIndex,
                    IsIncludedInLoad: true,
                    Diagnostics: diagnostics));
        }

        var directSolarGainW =
            input.AreaM2 *
            irradiance.DirectIrradianceWPerM2 *
            effectiveSolarFactor;
        var diffuseSolarGainW =
            input.AreaM2 *
            irradiance.DiffuseIrradianceWPerM2 *
            effectiveSolarFactor;
        var groundReflectedSolarGainW =
            input.AreaM2 *
            irradiance.GroundReflectedIrradianceWPerM2 *
            effectiveSolarFactor;

        var solarGainW =
            input.AreaM2 *
            irradiance.IncidentIrradianceWPerM2 *
            effectiveSolarFactor;

        return Result<WindowSolarGainResult>.Success(
            new WindowSolarGainResult(
                input.WindowId,
                input.RoomId,
                input.AreaM2,
                input.OrientationAzimuthDeg,
                input.TiltDeg,
                Round(irradiance.IncidentIrradianceWPerM2),
                Round(irradiance.DirectIrradianceWPerM2),
                Round(irradiance.DiffuseIrradianceWPerM2),
                Round(irradiance.GroundReflectedIrradianceWPerM2),
                shgc,
                frameFactor.Value,
                input.InternalShadingFactor,
                input.ExternalShadingFactor,
                input.FixedShadingFactor,
                Round(effectiveSolarFactor),
                Round(directSolarGainW),
                Round(diffuseSolarGainW),
                Round(groundReflectedSolarGainW),
                Round(solarGainW),
                input.HourIndex,
                IsIncludedInLoad: true,
                Diagnostics: diagnostics));
    }

    public Result<RoomWindowSolarGainResult> CalculateRoom(
        RoomWindowSolarGainRequest request)
    {
        if (request is null)
            return Result<RoomWindowSolarGainResult>.Validation("Room window solar gain request is required.");

        if (request.Windows is null)
            return Result<RoomWindowSolarGainResult>.Validation("Room window solar gain inputs are required.");

        var windows = new List<WindowSolarGainResult>(request.Windows.Count);
        var diagnostics = new List<SolarGainDiagnostic>();

        foreach (var window in request.Windows)
        {
            var result = Calculate(window);
            if (result.IsFailure)
                return Result<RoomWindowSolarGainResult>.Failure(result);

            windows.Add(result.Value);
            diagnostics.AddRange(result.Value.Diagnostics);
        }

        var included = windows.Where(window => window.IsIncludedInLoad).ToArray();
        var totalSolarGainW = included.Sum(window => window.SolarGainW);
        var hourlyGroups = included
            .Where(window => window.HourIndex.HasValue)
            .GroupBy(window => window.HourIndex!.Value)
            .Select(group => new
            {
                Hour = group.Key,
                SolarGainW = group.Sum(window => window.SolarGainW)
            })
            .OrderByDescending(group => group.SolarGainW)
            .ThenBy(group => group.Hour)
            .ToArray();

        return Result<RoomWindowSolarGainResult>.Success(
            new RoomWindowSolarGainResult(
                request.RoomId,
                Round(totalSolarGainW),
                windows,
                hourlyGroups.Length > 0 ? Round(hourlyGroups[0].SolarGainW) : null,
                hourlyGroups.Length > 0 ? hourlyGroups[0].Hour : null,
                diagnostics));
    }

    private static List<SolarGainDiagnostic> Validate(
        WindowSolarGainInput input)
    {
        var diagnostics = new List<SolarGainDiagnostic>();

        if (input.AreaM2 <= 0)
            diagnostics.Add(Error("SolarGains.InvalidArea", "Window area must be greater than zero.", input.DiagnosticsContext));

        if (!input.Shgc.HasValue)
        {
            diagnostics.Add(Error(
                "SolarGains.MissingShgc",
                "Solar heat gain coefficient is required for window solar gains.",
                input.DiagnosticsContext));
        }
        else if (input.Shgc.Value is < 0.0 or > 1.0)
        {
            diagnostics.Add(Error(
                "SolarGains.InvalidShgc",
                "Solar heat gain coefficient must be between 0 and 1.",
                input.DiagnosticsContext));
        }

        if (input.FrameFactor is < 0.0 or > 1.0)
            diagnostics.Add(Error("SolarGains.InvalidFrameFactor", "Frame factor must be between 0 and 1.", input.DiagnosticsContext));

        ValidateFactor(diagnostics, input.InternalShadingFactor, "SolarGains.InvalidInternalShadingFactor", "Internal shading factor must be between 0 and 1.", input.DiagnosticsContext);
        ValidateFactor(diagnostics, input.ExternalShadingFactor, "SolarGains.InvalidExternalShadingFactor", "External shading factor must be between 0 and 1.", input.DiagnosticsContext);
        ValidateFactor(diagnostics, input.FixedShadingFactor, "SolarGains.InvalidFixedShadingFactor", "Fixed shading factor must be between 0 and 1.", input.DiagnosticsContext);

        if (input.OrientationAzimuthDeg is < 0.0 or > 360.0)
            diagnostics.Add(Error("SolarGains.InvalidOrientation", "Window orientation azimuth must be between 0 and 360 degrees.", input.DiagnosticsContext));

        if (input.TiltDeg is < 0.0 or > 180.0)
            diagnostics.Add(Error("SolarGains.InvalidTilt", "Window tilt must be between 0 and 180 degrees.", input.DiagnosticsContext));

        ValidateIrradiance(diagnostics, input.IncidentIrradianceWPerM2, "SolarGains.InvalidIncidentIrradiance", "Incident irradiance cannot be negative.", input.DiagnosticsContext);
        ValidateIrradiance(diagnostics, input.DirectIrradianceWPerM2, "SolarGains.InvalidDirectIrradiance", "Direct irradiance cannot be negative.", input.DiagnosticsContext);
        ValidateIrradiance(diagnostics, input.DiffuseIrradianceWPerM2, "SolarGains.InvalidDiffuseIrradiance", "Diffuse irradiance cannot be negative.", input.DiagnosticsContext);
        ValidateIrradiance(diagnostics, input.GroundReflectedIrradianceWPerM2, "SolarGains.InvalidGroundReflectedIrradiance", "Ground-reflected irradiance cannot be negative.", input.DiagnosticsContext);

        return diagnostics;
    }

    private static WindowSolarIrradiance? ResolveIrradiance(
        WindowSolarGainInput input,
        List<SolarGainDiagnostic> diagnostics)
    {
        if (diagnostics.Any(diagnostic => diagnostic.Severity == SolarGainDiagnosticSeverity.Error))
            return null;

        var direct = input.DirectIrradianceWPerM2 ?? 0.0;
        var diffuse = input.DiffuseIrradianceWPerM2 ?? 0.0;
        var ground = input.GroundReflectedIrradianceWPerM2 ?? 0.0;
        var hasComponentIrradiance =
            input.DirectIrradianceWPerM2.HasValue ||
            input.DiffuseIrradianceWPerM2.HasValue ||
            input.GroundReflectedIrradianceWPerM2.HasValue;

        if (!input.IncidentIrradianceWPerM2.HasValue && !hasComponentIrradiance)
        {
            diagnostics.Add(Error(
                "SolarGains.MissingIrradiance",
                "Incident irradiance or component irradiance is required for window solar gains.",
                input.DiagnosticsContext));
            return null;
        }

        var incident = input.IncidentIrradianceWPerM2 ?? direct + diffuse + ground;

        if (!hasComponentIrradiance)
        {
            direct = incident;
            diffuse = 0.0;
            ground = 0.0;
        }

        diagnostics.Add(new SolarGainDiagnostic(
            SolarGainDiagnosticSeverity.Info,
            hasComponentIrradiance
                ? "SolarGains.ComponentIrradianceProvided"
                : "SolarGains.IncidentIrradianceProvided",
            hasComponentIrradiance
                ? "Window solar gains use provided direct, diffuse, and ground-reflected irradiance components."
                : "Window solar gains use provided incident irradiance.",
            input.DiagnosticsContext));

        return new WindowSolarIrradiance(
            incident,
            direct,
            diffuse,
            ground);
    }

    private static void ValidateFactor(
        List<SolarGainDiagnostic> diagnostics,
        double value,
        string code,
        string message,
        string? context)
    {
        if (value is < 0.0 or > 1.0)
            diagnostics.Add(Error(code, message, context));
    }

    private static void ValidateIrradiance(
        List<SolarGainDiagnostic> diagnostics,
        double? value,
        string code,
        string message,
        string? context)
    {
        if (value is < 0.0)
            diagnostics.Add(Error(code, message, context));
    }

    private static WindowSolarGainResult Excluded(
        WindowSolarGainInput input,
        double frameFactor,
        IReadOnlyList<SolarGainDiagnostic> diagnostics) =>
        new(
            input.WindowId,
            input.RoomId,
            Math.Max(input.AreaM2, 0),
            input.OrientationAzimuthDeg,
            input.TiltDeg,
            IncidentIrradianceWPerM2: 0,
            DirectIrradianceWPerM2: 0,
            DiffuseIrradianceWPerM2: 0,
            GroundReflectedIrradianceWPerM2: 0,
            Shgc: Math.Clamp(input.Shgc ?? 0, 0, 1),
            FrameFactor: Math.Clamp(frameFactor, 0, 1),
            Math.Clamp(input.InternalShadingFactor, 0, 1),
            Math.Clamp(input.ExternalShadingFactor, 0, 1),
            Math.Clamp(input.FixedShadingFactor, 0, 1),
            EffectiveSolarFactor: 0,
            DirectSolarGainW: 0,
            DiffuseSolarGainW: 0,
            GroundReflectedSolarGainW: 0,
            SolarGainW: 0,
            input.HourIndex,
            IsIncludedInLoad: false,
            diagnostics);

    private static SolarGainDiagnostic Error(
        string code,
        string message,
        string? context) =>
        new(SolarGainDiagnosticSeverity.Error, code, message, context);

    private static double Round(double value) =>
        Math.Round(value, 6, MidpointRounding.AwayFromZero);

    private sealed record WindowSolarIrradiance(
        double IncidentIrradianceWPerM2,
        double DirectIrradianceWPerM2,
        double DiffuseIrradianceWPerM2,
        double GroundReflectedIrradianceWPerM2);
}
